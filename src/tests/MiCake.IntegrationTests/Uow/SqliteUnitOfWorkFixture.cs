using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace MiCake.IntegrationTests.Uow
{
    /// <summary>
    /// File-backed SQLite acceptance fixture for the relational transaction matrix (t7).
    /// Each provider owns a unique temporary database file; every scope resolves its own
    /// DbContext through the frame-stable factory so transaction and rollback guarantees
    /// are exercised against a real relational store, never EF Core InMemory.
    /// </summary>
    public sealed class SqliteUnitOfWorkFixture : IDisposable
    {
        private readonly string _primaryDbPath;
        private readonly string _secondaryDbPath;

        public SqliteUnitOfWorkFixture()
        {
            _primaryDbPath = Path.Combine(Path.GetTempPath(), $"micake-uow-acceptance-{Guid.NewGuid():N}.db");
            _secondaryDbPath = Path.Combine(Path.GetTempPath(), $"micake-uow-acceptance-secondary-{Guid.NewGuid():N}.db");
        }

        /// <summary>
        /// Builds a service provider with the primary acceptance DbContext, the UoW runtime,
        /// the EF Core write pipeline, and (optionally) a secondary DbContext type used for
        /// late-resource-registration scenarios.
        /// </summary>
        public ServiceProvider BuildProvider(
            bool registerSecondaryContext = false,
            Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();

            services.AddDbContext<UowAcceptanceDbContext>((sp, opt) =>
                opt.UseSqlite($"Data Source={_primaryDbPath};Pooling=False"));
            services.ConfigureDbContext<UowAcceptanceDbContext>((sp, builder) =>
                builder.AddInterceptors(
                    sp.GetRequiredService<MiCakeEFCoreInterceptor>(),
                    sp.GetRequiredService<MiCakeDbCommandInterceptor>()));

            if (registerSecondaryContext)
            {
                services.AddDbContext<UowSecondaryDbContext>((sp, opt) =>
                    opt.UseSqlite($"Data Source={_secondaryDbPath};Pooling=False"));
                services.ConfigureDbContext<UowSecondaryDbContext>((sp, builder) =>
                {
                    builder.UseMiCake();
                    builder.AddInterceptors(
                        sp.GetRequiredService<MiCakeEFCoreInterceptor>(),
                        sp.GetRequiredService<MiCakeDbCommandInterceptor>());
                });
            }

            AddCoreServices(services, registerSecondaryContext);
            configure?.Invoke(services);

            var provider = services.BuildServiceProvider();
            RegisterDbContextTypes(provider, registerSecondaryContext);
            return provider;
        }

        /// <summary>
        /// Builds a provider whose primary DbContext is registered through
        /// <c>AddDbContextPool</c>, proving the pool-reset contract and frame isolation
        /// for pooled contexts against a real relational store. Microsoft.Data.Sqlite
        /// connection pooling stays enabled here on purpose: EF's context pooling keeps
        /// rented instances (and their connections) alive across scopes, and with
        /// <c>Pooling=False</c> each leased instance would accumulate a separate physical
        /// connection whose BEGIN IMMEDIATE contends for the file lock.
        /// When <paramref name="validateScopes"/> is set, the provider mirrors the ASP.NET
        /// Development host, where resolving scoped services from the pool root (captured
        /// at options-build time) must fail fast instead of silently leaking state.
        /// </summary>
        public ServiceProvider BuildPooledProvider(
            Action<IServiceCollection>? configure = null,
            bool validateScopes = false)
        {
            var services = new ServiceCollection();

            services.AddDbContextPool<UowAcceptanceDbContext>((sp, opt) =>
                opt.UseSqlite($"Data Source={_primaryDbPath}"));
            services.ConfigureDbContext<UowAcceptanceDbContext>((sp, builder) =>
            {
                builder.UseMiCake();
                builder.AddInterceptors(
                    sp.GetRequiredService<MiCakeEFCoreInterceptor>(),
                    sp.GetRequiredService<MiCakeDbCommandInterceptor>());
            });

            AddCoreServices(services, registerSecondaryContext: false);
            configure?.Invoke(services);

            var provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = validateScopes });
            RegisterDbContextTypes(provider, registerSecondaryContext: false);
            return provider;
        }

        private void AddCoreServices(IServiceCollection services, bool registerSecondaryContext)
        {
            services.AddLogging();
            services.AddUowCoreServices(typeof(UowAcceptanceDbContext));
            if (registerSecondaryContext)
            {
                services.AddUowCoreServices(typeof(UowSecondaryDbContext));
            }

            // The IntegrationTests assembly has InternalsVisibleTo access to the framework
            // packages, so the runtime internals are registered directly without reflection.
            services.AddSingleton<AmbientUnitOfWorkAccessor>();
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => sp.GetRequiredService<AmbientUnitOfWorkAccessor>());
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(UowAcceptanceDbContext)));
            services.AddSingleton<MiCakeEFCoreInterceptor>();
            services.AddSingleton<MiCakeDbCommandInterceptor>();

            // Repository for the primary aggregate.
            services.AddScoped<EFRepositoryDependencies<UowAcceptanceDbContext>>();
            services.AddScoped<IRepository<UowAcceptanceAggregate, int>, EFRepository<UowAcceptanceDbContext, UowAcceptanceAggregate, int>>();
        }

        private static void RegisterDbContextTypes(ServiceProvider provider, bool registerSecondaryContext)
        {
            provider.GetRequiredService<IDbContextTypeRegistry>().RegisterDbContextType(typeof(UowAcceptanceDbContext));
            if (registerSecondaryContext)
            {
                provider.GetRequiredService<IDbContextTypeRegistry>().RegisterDbContextType(typeof(UowSecondaryDbContext));
            }
        }

        /// <summary>
        /// Resolves the frame-stable primary DbContext for the current unit of work.
        /// Using the factory (rather than resolving the DbContext directly) is what registers
        /// the EF Core resource on the ambient unit of work; direct DI resolution bypasses it.
        /// </summary>
        public static UowAcceptanceDbContext GetPrimaryContext(IServiceProvider serviceProvider)
            => serviceProvider.GetRequiredService<IEFCoreContextFactory<UowAcceptanceDbContext>>().GetDbContext();

        /// <summary>
        /// Resolves the frame-stable secondary DbContext for the current unit of work.
        /// </summary>
        public static UowSecondaryDbContext GetSecondaryContext(IServiceProvider serviceProvider)
            => serviceProvider.GetRequiredService<IEFCoreContextFactory<UowSecondaryDbContext>>().GetDbContext();

        public void Dispose()
        {
            DeleteIfExists(_primaryDbPath);
            DeleteIfExists(_secondaryDbPath);
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // A lingering connection may still hold the file; the OS temp cleaner handles it.
            }
        }
    }

    /// <summary>
    /// Aggregate root used by the acceptance matrix. The integer identity is
    /// database-generated by SQLite, which is required for the generated-key flush scenario.
    /// </summary>
    public class UowAcceptanceAggregate : AggregateRoot<int>
    {
        public string Name { get; private set; }
        public string Version { get; private set; }

        protected UowAcceptanceAggregate()
        {
            Version = Guid.NewGuid().ToString("N");
        }

        public UowAcceptanceAggregate(string name) : this()
        {
            Name = name;
        }

        public void ChangeName(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Primary DbContext for the acceptance matrix. Uses the MiCake base so conventions,
    /// domain events, and audit configuration are exercised through the real pipeline.
    /// </summary>
    public class UowAcceptanceDbContext : MiCakeDbContext
    {
        public DbSet<UowAcceptanceAggregate> Aggregates { get; set; }

        public UowAcceptanceDbContext(DbContextOptions<UowAcceptanceDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // The base call is safe: the provider-less fallback interceptors are skipped
            // when the provider-bound interceptors are already wired through AddDbContext.
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UowAcceptanceAggregate>(e =>
            {
                // Optimistic concurrency token for the detached stale replacement scenario.
                e.Property(x => x.Version).IsConcurrencyToken();
            });
        }
    }

    /// <summary>
    /// Simple entity used to register a second, independent resource inside one unit of work
    /// so savepoint coverage and late-resource-registration rejection can be proven.
    /// </summary>
    public class UowSecondaryEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Secondary DbContext type used for multi-resource and late-registration scenarios.
    /// </summary>
    public class UowSecondaryDbContext : MiCakeDbContext
    {
        public DbSet<UowSecondaryEntity> Entities { get; set; }

        public UowSecondaryDbContext(DbContextOptions<UowSecondaryDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // The base call is safe: the provider-less fallback interceptors are skipped
            // when the provider-bound interceptors are already wired through AddDbContext.
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
