using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Repository
{
    /// <summary>
    /// t5 repository contract tests: tracked lifecycle deletion by id, explicit physical
    /// deletion inside the UoW transaction, detached stale replacement concurrency, and
    /// no-op deletion of missing aggregates.
    /// </summary>
    public class EFRepositoryContractTests : IDisposable
    {
        private readonly string _dbPath;

        public EFRepositoryContractTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"micake-repo-contract-{Guid.NewGuid():N}.db");
            SoftDeletePreSaveHandler.Reset();
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        private ServiceProvider BuildProvider(Action<IServiceCollection>? registerHandlers = null, bool installInterceptors = true)
        {
            var services = new ServiceCollection();
            services.AddDbContext<RepoContractDbContext>((sp, opt) =>
            {
                opt.UseSqlite($"Data Source={_dbPath};Pooling=False");
                if (installInterceptors)
                {
                    opt.UseMiCakeInterceptors(sp);
                }
            });
            services.AddLogging();
            services.AddUowCoreServices(typeof(RepoContractDbContext));

            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(RepoContractDbContext)));
            services.AddSingleton<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();

            registerHandlers?.Invoke(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IDbContextTypeRegistry>().RegisterDbContextType(typeof(RepoContractDbContext));
            return provider;
        }

        private static EFRepository<RepoContractDbContext, RepoContractEntity, int> GetRepository(IServiceProvider scope)
        {
            var dependencies = scope.GetRequiredService<EFRepositoryDependencies<RepoContractDbContext>>();
            return new EFRepository<RepoContractDbContext, RepoContractEntity, int>(dependencies);
        }

        [Fact]
        public async Task DeleteByIdAsync_TriggersLifecycle_SoftDeleteHandlerRuns()
        {
            using var provider = BuildProvider(s =>
                s.AddScoped<IRepositoryPreSaveChanges, SoftDeletePreSaveHandler>());
            await using (var setupScope = provider.CreateAsyncScope())
            {
                await setupScope.ServiceProvider.GetRequiredService<RepoContractDbContext>().Database.EnsureCreatedAsync();
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RepoContractDbContext>();
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var repository = GetRepository(scope.ServiceProvider);

                await using var uow1 = await manager.BeginAsync();
                var entity = new RepoContractEntity { Name = "target" };
                await repository.AddAsync(entity);
                await uow1.CommitAsync();
                var id = entity.Id;

                // Delete by id inside a second unit of work: tracked lifecycle deletion.
                await using var uow2 = await manager.BeginAsync();
                await repository.DeleteByIdAsync(id);
                await uow2.CommitAsync();

                // The pre-save handler observed the Deleted state and converted it to a
                // soft delete, exactly as it would for DeleteAsync.
                Assert.Equal(1, SoftDeletePreSaveHandler.CallCount);
                var remaining = await context.Entities.CountAsync(e => e.IsDeleted);
                Assert.Equal(1, remaining);
            }
        }

        [Fact]
        public async Task DeleteByIdAsync_MissingId_IsNoOp()
        {
            using var provider = BuildProvider();
            await using (var setupScope = provider.CreateAsyncScope())
            {
                await setupScope.ServiceProvider.GetRequiredService<RepoContractDbContext>().Database.EnsureCreatedAsync();
            }

            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<RepoContractDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var repository = GetRepository(scope.ServiceProvider);

            await using var uow = await manager.BeginAsync();
            await repository.DeleteByIdAsync(12345);
            await uow.CommitAsync();

            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task PhysicalDelete_BypassesLifecycle_AndRollsBackWithUoW()
        {
            using var provider = BuildProvider(s =>
                s.AddScoped<IRepositoryPreSaveChanges, SoftDeletePreSaveHandler>());

            // Seed two rows through a committed unit of work.
            await using (var seedScope = provider.CreateAsyncScope())
            {
                await seedScope.ServiceProvider.GetRequiredService<RepoContractDbContext>().Database.EnsureCreatedAsync();
                var manager = seedScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var repository = GetRepository(seedScope.ServiceProvider);

                await using var uow = await manager.BeginAsync();
                await repository.AddAsync(new RepoContractEntity { Name = "a" });
                await repository.AddAsync(new RepoContractEntity { Name = "b" });
                await uow.CommitAsync();
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RepoContractDbContext>();
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var executor = scope.ServiceProvider.GetRequiredService<IEFCorePhysicalOperationExecutor<RepoContractDbContext>>();

                // Rollback path: the physical delete does not run lifecycle handlers and
                // is undone by the unit of work rollback.
                await using (var uow = await manager.BeginAsync())
                {
                    await executor.ExecuteDeleteAsync<RepoContractEntity>(e => e.Name == "a");
                    Assert.Equal(0, SoftDeletePreSaveHandler.CallCount);
                    Assert.Equal(1, await context.Entities.CountAsync());
                    await uow.RollbackAsync();
                }

                Assert.Equal(2, await context.Entities.CountAsync());

                // Commit path: the physical delete becomes durable.
                await using (var uow = await manager.BeginAsync())
                {
                    await executor.ExecuteDeleteAsync<RepoContractEntity>(e => e.Name == "a");
                    await uow.CommitAsync();
                }

                Assert.Equal(1, await context.Entities.CountAsync());
                Assert.Equal(0, SoftDeletePreSaveHandler.CallCount);
            }
        }

        [Fact]
        public async Task PhysicalDelete_WithoutUoW_Throws()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var executor = scope.ServiceProvider.GetRequiredService<IEFCorePhysicalOperationExecutor<RepoContractDbContext>>();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                executor.ExecuteDeleteAsync<RepoContractEntity>(e => e.Name == "x"));

            Assert.Contains("active writable unit of work", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PhysicalDelete_ReadOnlyUoW_Throws()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var executor = scope.ServiceProvider.GetRequiredService<IEFCorePhysicalOperationExecutor<RepoContractDbContext>>();

            await using var uow = await manager.BeginAsync(UnitOfWorkOptions.ReadOnly);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                executor.ExecuteDeleteAsync<RepoContractEntity>(e => e.Name == "x"));

            Assert.Contains("read-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PhysicalDelete_WithoutCommandInterceptor_StillRollsBackWithUoW()
        {
            using var provider = BuildProvider(installInterceptors: false);

            await using (var seedScope = provider.CreateAsyncScope())
            {
                await seedScope.ServiceProvider.GetRequiredService<RepoContractDbContext>().Database.EnsureCreatedAsync();
                var manager = seedScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var repository = GetRepository(seedScope.ServiceProvider);

                await using var uow = await manager.BeginAsync();
                await repository.AddAsync(new RepoContractEntity { Name = "a" });
                await repository.AddAsync(new RepoContractEntity { Name = "b" });
                await uow.FlushAsync();
                await uow.CommitAsync();
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RepoContractDbContext>();
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var executor = scope.ServiceProvider.GetRequiredService<IEFCorePhysicalOperationExecutor<RepoContractDbContext>>();

                await using (var uow = await manager.BeginAsync())
                {
                    await executor.ExecuteDeleteAsync<RepoContractEntity>(e => e.Name == "a");
                    Assert.Equal(1, await context.Entities.CountAsync());
                    await uow.RollbackAsync();
                }

                Assert.Equal(2, await context.Entities.CountAsync());
            }
        }

        [Fact]
        public async Task UpdateAsync_DetachedStaleReplacement_ThrowsConcurrencyConflict()
        {
            using var provider = BuildProvider();
            await using (var setupScope = provider.CreateAsyncScope())
            {
                await setupScope.ServiceProvider.GetRequiredService<RepoContractDbContext>().Database.EnsureCreatedAsync();
            }

            int id;
            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var repository = GetRepository(scope.ServiceProvider);

                await using var uow = await manager.BeginAsync();
                var entity = new RepoContractEntity { Name = "original", Version = 1 };
                await repository.AddAsync(entity);
                await uow.CommitAsync();
                id = entity.Id;
            }

            // A concurrent save advances the version (through the repository so the
            // context is registered as a UoW resource and the commit actually flushes).
            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var repository = GetRepository(scope.ServiceProvider);

                await using var uow = await manager.BeginAsync();
                var tracked = await repository.FindAsync(id);
                tracked!.Name = "concurrent";
                tracked.Version = 2;
                await uow.CommitAsync();
            }

            // A detached stale aggregate (old version) must not silently overwrite it.
            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var repository = GetRepository(scope.ServiceProvider);
                var stale = new RepoContractEntity { Id = id, Name = "stale", Version = 1 };

                await using var uow = await manager.BeginAsync();
                await repository.UpdateAsync(stale);

                var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => uow.CommitAsync());
                Assert.NotNull(exception);
            }
        }

        #region Test Handlers

        /// <summary>
        /// Converts tracked deletions into soft deletes so lifecycle invocation is observable.
        /// </summary>
        public class SoftDeletePreSaveHandler : IRepositoryPreSaveChanges
        {
            public static int CallCount;
            public static void Reset() => CallCount = 0;

            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                if (entity is RepoContractEntity repoEntity && entityState == RepositoryEntityStates.Deleted)
                {
                    Interlocked.Increment(ref CallCount);
                    repoEntity.IsDeleted = true;
                    return new ValueTask<RepositoryEntityStates>(RepositoryEntityStates.Modified);
                }

                return new ValueTask<RepositoryEntityStates>(entityState);
            }
        }

        #endregion

        public class RepoContractEntity : AggregateRoot<int>
        {
            public string Name { get; set; } = string.Empty;
            public bool IsDeleted { get; set; }
            public int Version { get; set; }
        }

        public class RepoContractDbContext : DbContext
        {
            public RepoContractDbContext(DbContextOptions<RepoContractDbContext> options) : base(options)
            {
            }

            public DbSet<RepoContractEntity> Entities => Set<RepoContractEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<RepoContractEntity>()
                    .Property(e => e.Version)
                    .IsConcurrencyToken();
            }
        }
    }
}
