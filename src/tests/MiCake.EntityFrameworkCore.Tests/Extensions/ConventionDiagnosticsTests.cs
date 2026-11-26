using MiCake.Audit.SoftDeletion;
using MiCake.Core;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Store;
using MiCake.EntityFrameworkCore.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Extensions
{
    [Collection("MiCakeStaticFactory")]
    public class ConventionDiagnosticsTests : IDisposable
    {
        private const string Category = "MiCake.EntityFrameworkCore.Conventions";

        private readonly List<ServiceProvider> _providers = new();

        public ConventionDiagnosticsTests()
        {
            MiCakeConventionEngineProvider.Clear();
        }

        public void Dispose()
        {
            MiCakeConventionEngineProvider.Clear();
            foreach (var provider in _providers)
            {
                provider.Dispose();
            }
        }

        [Fact]
        public void EntityLevelFailure_LogsWarningWithExceptionAndContinues()
        {
            SetEngine(new ThrowingEntityConvention(typeof(DiagEntityLevelEntity)));
            var logs = new CapturingLoggerFactory();

            using var context = NewContext<DiagEntityLevelEntity>(logs);
            var model = context.Model;

            Assert.NotNull(model.FindEntityType(typeof(DiagEntityLevelEntity)));
            var entry = Assert.Single(logs.Entries, e => e.Level == LogLevel.Warning);
            Assert.Equal(Category, entry.Category);
            Assert.Equal("Failed to apply entity-level conventions for {EntityType}", entry.Template);
            Assert.Contains(nameof(DiagEntityLevelEntity), entry.Message);
            Assert.IsType<InvalidOperationException>(entry.Exception);
        }

        [Fact]
        public void PropertyLevelFailure_LogsWarningNamingEntityAndProperty()
        {
            SetEngine(new ThrowingPropertyConvention(typeof(DiagPropertyEntity), nameof(DiagPropertyEntity.Name)));
            var logs = new CapturingLoggerFactory();

            using var context = NewContext<DiagPropertyEntity>(logs);
            _ = context.Model;

            var entry = Assert.Single(logs.Entries, e => e.Level == LogLevel.Warning);
            Assert.Equal("Failed to apply property conventions for {EntityType}.{PropertyName}", entry.Template);
            Assert.Contains($"{nameof(DiagPropertyEntity)}.{nameof(DiagPropertyEntity.Name)}", entry.Message);
            Assert.NotNull(entry.Exception);
        }

        [Fact]
        public void PerEntityFailure_LogsWarningWithException()
        {
            SetEngine(new ThrowingCanApplyConvention(typeof(DiagOuterEntity)));
            var logs = new CapturingLoggerFactory();

            using var context = NewContext<DiagOuterEntity>(logs);
            _ = context.Model;

            var entry = Assert.Single(logs.Entries, e => e.Level == LogLevel.Warning);
            Assert.Equal("Failed to apply conventions for {EntityType}", entry.Template);
            Assert.Contains(nameof(DiagOuterEntity), entry.Message);
            Assert.NotNull(entry.Exception);
        }

        [Fact]
        public void OwnedType_LogsOneDebugEntryAndNoWarning()
        {
            SetEngine(new DefaultValueConvention(typeof(DiagAddress), nameof(DiagAddress.City)));
            var logs = new CapturingLoggerFactory();

            var options = new DbContextOptionsBuilder<DiagOwnedDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new DiagOwnedDbContext(options, logs);
            _ = context.Model;

            Assert.DoesNotContain(logs.Entries, e => e.Level >= LogLevel.Warning);
            var entry = Assert.Single(logs.Entries, e => e.Level == LogLevel.Debug);
            Assert.Equal("Skipping conventions for {EntityType}: configured as owned or complex type", entry.Template);
            Assert.Contains(nameof(DiagAddress), entry.Message);
        }

        [Fact]
        public void OwnedTypeSharedByTwoOwners_LogsNoWarning()
        {
            SetEngine(new DefaultValueConvention(typeof(DiagSharedAddress), nameof(DiagSharedAddress.City)));
            var logs = new CapturingLoggerFactory();

            var options = new DbContextOptionsBuilder<DiagSharedOwnedDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new DiagSharedOwnedDbContext(options, logs);
            _ = context.Model;

            Assert.DoesNotContain(logs.Entries, e => e.Level >= LogLevel.Warning);
            Assert.Contains(logs.Entries, e => e.Level == LogLevel.Debug
                && e.Template == "Skipping conventions for {EntityType}: configured as owned or complex type"
                && e.Message.Contains(nameof(DiagSharedAddress)));
        }

        [Fact]
        public void MissingEngine_LogsWarningAndAppliesNoFilter()
        {
            var logs = new CapturingLoggerFactory();

            using var context = NewContext<DiagMissingEngineEntity>(logs);
            var entityType = context.Model.FindEntityType(typeof(DiagMissingEngineEntity));

            Assert.NotNull(entityType);
            Assert.Empty(entityType.GetDeclaredQueryFilters());
            var entry = Assert.Single(logs.Entries);
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Equal(
                "No MiCake convention engine is configured; conventions, including the soft-delete query filter, were not applied to {EntityTypeCount} entity types",
                entry.Template);
        }

        [Fact]
        public void MissingEngine_WithoutAnyLoggerSource_DoesNotThrow()
        {
            var options = new DbContextOptionsBuilder<DiagNoLoggerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new DiagNoLoggerDbContext(options);

            Assert.NotNull(context.Model.FindEntityType(typeof(DiagNoLoggerEntity)));
        }

        [Fact]
        public void MiCakeDbContext_FromAddDbContext_LogsToApplicationLogging()
        {
            var logs = new CapturingLoggerFactory();
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddProvider(logs));
            services.AddDbContext<DiagAddDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<DiagAddDbContext>().Model;

            var entry = Assert.Single(logs.Entries, e => e.Category == Category);
            Assert.Equal(LogLevel.Warning, entry.Level);
        }

        [Fact]
        public void MiCakeDbContext_WithUseLoggerFactory_LogsToThatFactory()
        {
            var logs = new CapturingLoggerFactory();
            var options = new DbContextOptionsBuilder<DiagUseLoggerFactoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseLoggerFactory(logs)
                .Options;

            using var context = new DiagUseLoggerFactoryDbContext(options);
            _ = context.Model;

            Assert.Single(logs.Entries, e => e.Category == Category && e.Level == LogLevel.Warning);
        }

        [Fact]
        public void MiCakeDbContext_WithoutOptionsSource_FallsBackToStoredLogger()
        {
            var stored = new CapturingLoggerFactory();
            MiCakeConventionEngineProvider.SetLogger(stored.CreateLogger(Category));
            var options = new DbContextOptionsBuilder<DiagStoredLoggerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new DiagStoredLoggerDbContext(options);
            _ = context.Model;

            Assert.Single(stored.Entries, e => e.Level == LogLevel.Warning);
        }

        [Fact]
        public void ModuleShutdown_ClearsOnlyItsOwnStoredLogger()
        {
            var older = new CapturingLoggerFactory();
            var newer = new CapturingLoggerFactory();
            var olderModule = new MiCakeEFCoreModule();
            var newerModule = new MiCakeEFCoreModule();

            olderModule.OnApplicationInitialization(InitializationContextWith(older));
            newerModule.OnApplicationInitialization(InitializationContextWith(newer));

            olderModule.OnApplicationShutdown(ShutdownContext());
            MiCakeConventionEngineProvider.GetLogger().LogInformation("after older shutdown");
            Assert.Empty(older.Entries);
            Assert.Single(newer.Entries, e => e.Message == "after older shutdown");

            newerModule.OnApplicationShutdown(ShutdownContext());
            MiCakeConventionEngineProvider.GetLogger().LogInformation("after newer shutdown");
            Assert.DoesNotContain(newer.Entries, e => e.Message == "after newer shutdown");
        }

        private static void SetEngine(params IStoreConvention[] conventions)
        {
            var engine = new StoreConventionEngine();
            foreach (var convention in conventions)
            {
                engine.AddConvention(convention);
            }

            MiCakeConventionEngineProvider.SetConventionEngine(engine);
        }

        private static DiagContext<TEntity> NewContext<TEntity>(ILoggerFactory loggerFactory) where TEntity : class
        {
            var options = new DbContextOptionsBuilder<DiagContext<TEntity>>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new DiagContext<TEntity>(options, loggerFactory);
        }

        private ModuleInitializationContext InitializationContextWith(ILoggerFactory loggerFactory)
        {
            var services = new ServiceCollection();
            services.AddSingleton(loggerFactory);
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(DbContext)));
            return new ModuleInitializationContext(
                TrackedProvider(services),
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());
        }

        private ModuleShutdownContext ShutdownContext()
        {
            return new ModuleShutdownContext(
                TrackedProvider(new ServiceCollection()),
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());
        }

        private ServiceProvider TrackedProvider(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            _providers.Add(provider);
            return provider;
        }
    }

    public sealed record CapturedLogEntry(string Category, LogLevel Level, string Message, string Template, Exception Exception);

    public sealed class CapturingLoggerFactory : ILoggerFactory, ILoggerProvider
    {
        private readonly object _gate = new();
        private readonly List<CapturedLogEntry> _entries = new();

        public IReadOnlyList<CapturedLogEntry> Entries
        {
            get
            {
                lock (_gate)
                {
                    return _entries.ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }

        private void Add(CapturedLogEntry entry)
        {
            lock (_gate)
            {
                _entries.Add(entry);
            }
        }

        private sealed class CapturingLogger(CapturingLoggerFactory owner, string category) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var template = (state as IEnumerable<KeyValuePair<string, object>>)?
                    .FirstOrDefault(p => p.Key == "{OriginalFormat}").Value as string;
                owner.Add(new CapturedLogEntry(category, logLevel, formatter(state, exception), template, exception));
            }
        }
    }

    public class ThrowingEntityConvention(Type target) : IEntityConvention
    {
        public int Priority => 10;

        public bool CanApply(Type entityType) => entityType == target;

        public void Configure(Type entityType, EntityConventionContext context) => throw new InvalidOperationException("entity convention failed");
    }

    public class ThrowingPropertyConvention(Type target, string propertyName) : IPropertyConvention
    {
        public int Priority => 10;

        public bool CanApply(Type entityType) => entityType == target;

        public void Configure(Type entityType, string name, PropertyConventionContext context)
        {
            if (name == propertyName)
            {
                throw new InvalidOperationException("property convention failed");
            }
        }
    }

    public class ThrowingCanApplyConvention(Type target) : IStoreConvention
    {
        public int Priority => 10;

        public bool CanApply(Type entityType) => entityType == target
            ? throw new InvalidOperationException("can-apply failed")
            : false;
    }

    public class DefaultValueConvention(Type target, string propertyName) : IPropertyConvention
    {
        public int Priority => 10;

        public bool CanApply(Type entityType) => entityType == target;

        public void Configure(Type entityType, string name, PropertyConventionContext context)
        {
            if (name == propertyName)
            {
                context.HasDefaultValue = true;
                context.DefaultValue = "unknown";
            }
        }
    }

    public class DiagEntityLevelEntity : Entity
    {
        public string Name { get; set; }
    }

    public class DiagPropertyEntity : Entity
    {
        public string Name { get; set; }
    }

    public class DiagOuterEntity : Entity
    {
        public string Name { get; set; }
    }

    public class DiagMissingEngineEntity : Entity, ISoftDeletable
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DiagNoLoggerEntity : Entity
    {
        public string Name { get; set; }
    }

    public class DiagAddress : ValueObject
    {
        public string City { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return City ?? string.Empty;
        }
    }

    public class DiagOwnerEntity : Entity
    {
        public DiagAddress Address { get; set; }
    }

    public class DiagSharedAddress : ValueObject
    {
        public string City { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return City ?? string.Empty;
        }
    }

    public class DiagShippingOwner : Entity
    {
        public DiagSharedAddress Address { get; set; }
    }

    public class DiagBillingOwner : Entity
    {
        public DiagSharedAddress Address { get; set; }
    }

    public class DiagSharedOwnedDbContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;

        public DiagSharedOwnedDbContext(DbContextOptions<DiagSharedOwnedDbContext> options, ILoggerFactory loggerFactory) : base(options)
        {
            _loggerFactory = loggerFactory;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiagShippingOwner>().OwnsOne(e => e.Address);
            modelBuilder.Entity<DiagBillingOwner>().OwnsOne(e => e.Address);
            modelBuilder.UseMiCakeConventions(_loggerFactory);
        }
    }

    // EF Core caches one model per context type, so every scenario needs its own TEntity.
    public class DiagContext<TEntity> : DbContext where TEntity : class
    {
        private readonly ILoggerFactory _loggerFactory;

        public DiagContext(DbContextOptions<DiagContext<TEntity>> options, ILoggerFactory loggerFactory) : base(options)
        {
            _loggerFactory = loggerFactory;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TEntity>();
            modelBuilder.UseMiCakeConventions(_loggerFactory);
        }
    }

    public class DiagOwnedDbContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;

        public DiagOwnedDbContext(DbContextOptions<DiagOwnedDbContext> options, ILoggerFactory loggerFactory) : base(options)
        {
            _loggerFactory = loggerFactory;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiagOwnerEntity>().OwnsOne(e => e.Address);
            modelBuilder.UseMiCakeConventions(_loggerFactory);
        }
    }

    public class DiagNoLoggerDbContext(DbContextOptions<DiagNoLoggerDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiagNoLoggerEntity>();
            modelBuilder.UseMiCakeConventions();
        }
    }

    public class DiagAddDbContext(DbContextOptions<DiagAddDbContext> options) : MiCakeDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiagMissingEngineEntity>();
            base.OnModelCreating(modelBuilder);
        }
    }

    public class DiagUseLoggerFactoryDbContext(DbContextOptions<DiagUseLoggerFactoryDbContext> options) : MiCakeDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiagMissingEngineEntity>();
            base.OnModelCreating(modelBuilder);
        }
    }

    public class DiagStoredLoggerDbContext(DbContextOptions<DiagStoredLoggerDbContext> options) : MiCakeDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiagMissingEngineEntity>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
