using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Internal
{
    /// <summary>
    /// Tests for the write coordinator: missing/read-only unit of work rejection,
    /// transaction activation before writes, and provider transaction binding.
    /// </summary>
    public class EFCoreWriteCoordinatorTests : IDisposable
    {
        private readonly string _dbPath;

        public EFCoreWriteCoordinatorTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"micake-coordinator-{Guid.NewGuid():N}.db");
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        private ServiceProvider BuildProvider(bool registerInRegistry = true, Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();
            services.AddDbContext<WriteGuardTestDbContext>(opt =>
                opt.UseSqlite($"Data Source={_dbPath};Pooling=False"));
            services.AddLogging();
            services.AddUowCoreServices(typeof(WriteGuardTestDbContext));

            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(WriteGuardTestDbContext)));

            configure?.Invoke(services);

            var provider = services.BuildServiceProvider();
            if (registerInRegistry)
            {
                var registry = provider.GetRequiredService<IDbContextTypeRegistry>();
                registry.RegisterDbContextType(typeof(WriteGuardTestDbContext));
            }

            return provider;
        }

        [Fact]
        public async Task BeforeWriteAsync_WithoutUoW_ShouldThrow()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges).AsTask());

            Assert.Contains("active writable unit of work", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BeforeWriteAsync_ReadOnlyUoW_ShouldThrow()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            await using var uow = await uowManager.BeginAsync(UnitOfWorkOptions.ReadOnly);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges).AsTask());

            Assert.Contains("read-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BeforeWriteAsync_WithWritableUoW_ShouldActivateTransactionAndCommitPersists()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();

            // Act
            await coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges);
            context.Add(new WriteGuardEntity { Name = "committed" });
            await uow.CommitAsync();

            // Assert
            Assert.Equal(1, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task BeforeWriteAsync_WithWritableUoW_ThenRollback_ShouldDiscardWrites()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();

            await coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges);
            context.Add(new WriteGuardEntity { Name = "discarded" });
            await uow.RollbackAsync();

            // Assert
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task BeforeCommand_ShouldBindProviderTransactionToCommand()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();

            var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT 1";

            // Act
            coordinator.BeforeCommand(context, command, EFWriteOperationKind.RawSql);

            // Assert - the command is bound to the activated provider transaction
            Assert.NotNull(command.Transaction);
            Assert.Same(context.Database.CurrentTransaction!.GetDbTransaction(), command.Transaction);
        }

        [Fact]
        public async Task BeforeCommand_ConnectionMismatch_ShouldThrowBeforeExecution()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            await using var uow = await uowManager.BeginAsync();

            using var otherConnection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            otherConnection.Open();
            var command = otherConnection.CreateCommand();
            command.CommandText = "SELECT 1";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                coordinator.BeforeCommand(context, command, EFWriteOperationKind.RawSql));

            Assert.Contains("connection", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BeforeCommand_DatabaseInitialization_WithoutUoW_PassesUnbound()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();

            var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS Dummy (Id INTEGER)";

            // Database-initiation DDL without an ambient unit of work passes through unbound.
            coordinator.BeforeCommand(context, command, EFWriteOperationKind.DatabaseInitialization);
            Assert.Null(command.Transaction);
        }

        [Fact]
        public async Task BeforeCommand_UnknownNonQuery_WithoutUoW_ShouldThrow()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();

            var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "UPDATE Entities SET Name = 'x'";

            // Unknown non-query commands are treated conservatively as writes and rejected
            // without an active unit of work.
            var exception = Assert.Throws<InvalidOperationException>(() =>
                coordinator.BeforeCommand(context, command, EFWriteOperationKind.UnknownNonQuery));

            Assert.Contains("active writable unit of work", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Classify_CommandSources_MapsCorrectly()
        {
            Assert.Equal(EFWriteOperationKind.DatabaseInitialization, MiCakeDbCommandInterceptor.Classify(CommandSource.Migrations));
            Assert.Equal(EFWriteOperationKind.RawSql, MiCakeDbCommandInterceptor.Classify(CommandSource.ExecuteSqlRaw));
            Assert.Equal(EFWriteOperationKind.ExecuteDelete, MiCakeDbCommandInterceptor.Classify(CommandSource.ExecuteDelete));
            Assert.Equal(EFWriteOperationKind.UnknownNonQuery, MiCakeDbCommandInterceptor.Classify((CommandSource)int.MaxValue));
        }

        [Fact]
        public async Task BeforeWriteAsync_UnregisteredDbContext_ShouldThrow()
        {
            using var provider = BuildProvider(registerInRegistry: false);
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            await using var uow = await uowManager.BeginAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges).AsTask());

            Assert.Contains("not registered", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BeforeWriteAsync_LegacyCustomFactory_ReturningSameContext_WrapsAndActivatesTransaction()
        {
            // A legacy custom factory without the anchoring capability resolves its own
            // wrapper; when it wraps the same DbContext that performs the write, the
            // coordinator must register it with the unit of work and activate the
            // transaction on that context so the write commits with the UoW.
            using var provider = BuildProvider(configure: s =>
                s.AddScoped(typeof(IEFCoreContextFactory<WriteGuardTestDbContext>),
                    sp => new LegacyWriteGuardContextFactory(sp, returnDifferentContext: false)));
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();

            // Act - the write is guarded through the legacy factory's wrapper
            await coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges);
            context.Add(new WriteGuardEntity { Name = "legacy-factory" });
            await uow.CommitAsync();

            // Assert - the transaction was activated on the writing context and the
            // write committed with the unit of work
            await using var verifyScope = provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            Assert.Equal(1, await verifyContext.Entities.CountAsync());
        }

        [Fact]
        public async Task BeforeWriteAsync_LegacyCustomFactory_ReturningDifferentContext_RejectedBeforeTransactionActivation()
        {
            // A legacy custom factory whose wrapper is bound to a different DbContext
            // instance would activate the transaction on the wrong context and leave the
            // current write unbound; it must be rejected before any transaction starts.
            using var provider = BuildProvider(configure: s =>
                s.AddScoped(typeof(IEFCoreContextFactory<WriteGuardTestDbContext>),
                    sp => new LegacyWriteGuardContextFactory(sp, returnDifferentContext: true)));
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            await using var uow = await uowManager.BeginAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges).AsTask());

            Assert.Contains("different DbContext instance", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Null(context.Database.CurrentTransaction);
        }

        [Fact]
        public async Task BeforeWriteAsync_CustomFactory_TwoWritesSameContext_ReusesSingleResource()
        {
            // A custom factory that does not cache its wrapper resolves a new wrapper per
            // call; the coordinator must reuse the already-registered resource for the same
            // context so the second write does not register a second resource (which would
            // activate a second transaction on one store and fail).
            using var provider = BuildProvider(configure: s =>
                s.AddScoped(typeof(IEFCoreContextFactory<WriteGuardTestDbContext>),
                    sp => new LegacyWriteGuardContextFactory(sp, returnDifferentContext: false)));
            await using var scope = provider.CreateAsyncScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IEFCoreWriteCoordinator>();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();

            // Act - two guarded writes on the same context through the non-caching factory
            await coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges);
            await coordinator.BeforeWriteAsync(context, EFWriteOperationKind.SaveChanges);
            context.Add(new WriteGuardEntity { Name = "twice" });
            await uow.CommitAsync();

            // Assert - the second write reused the registered resource and the commit succeeded
            await using var verifyScope = provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            Assert.Equal(1, await verifyContext.Entities.CountAsync());
        }

        [Fact]
        public async Task ImmediateUoW_WithCustomFactory_ActivatesTransactionOnResolvedContext()
        {
            // A custom factory adapted through the internal runtime view must be validated
            // and registered by the immediate initializer so its transaction is activated
            // eagerly instead of being silently skipped.
            using var provider = BuildProvider(configure: s =>
                s.AddScoped(typeof(IEFCoreContextFactory<WriteGuardTestDbContext>),
                    sp => new LegacyWriteGuardContextFactory(sp, returnDifferentContext: false)));
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            // Act - Immediate mode initializes every registered factory at UoW creation
            await using var uow = await uowManager.BeginAsync(UnitOfWorkOptions.Immediate);

            // Assert - the custom factory's resource was registered and activated
            Assert.True(uow.HasActiveTransactions);
            Assert.NotNull(context.Database.CurrentTransaction);
        }

        [Fact]
        public async Task ImmediateUoW_WithCustomFactoryReturningDifferentContext_RejectedBeforeActivation()
        {
            // A custom factory whose wrapper is bound to a different DbContext instance
            // must be rejected during immediate initialization, before any transaction
            // is activated on either context.
            using var provider = BuildProvider(configure: s =>
                s.AddScoped(typeof(IEFCoreContextFactory<WriteGuardTestDbContext>),
                    sp => new LegacyWriteGuardContextFactory(sp, returnDifferentContext: true)));
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                uowManager.BeginAsync(UnitOfWorkOptions.Immediate));
            Assert.Contains("different DbContext instance", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Null(context.Database.CurrentTransaction);
        }
    }

    /// <summary>
    /// Custom factory implementation that resolves its wrapper from its own captured
    /// provider, optionally returning a wrapper for a different context instance
    /// (simulating a factory that resolves from the wrong scope/provider). It does not
    /// register the returned wrapper with the unit of work itself.
    /// </summary>
    internal sealed class LegacyWriteGuardContextFactory : IEFCoreContextFactory<WriteGuardTestDbContext>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _returnDifferentContext;

        public LegacyWriteGuardContextFactory(IServiceProvider serviceProvider, bool returnDifferentContext)
        {
            _serviceProvider = serviceProvider;
            _returnDifferentContext = returnDifferentContext;
        }

        public WriteGuardTestDbContext GetDbContext()
            => _serviceProvider.GetRequiredService<WriteGuardTestDbContext>();

        public EFCoreDbContextWrapper GetOrCreateWrapperFor(DbContext context)
        {
            if (_returnDifferentContext)
            {
                var options = _serviceProvider.GetRequiredService<DbContextOptions<WriteGuardTestDbContext>>();
                var other = new WriteGuardTestDbContext(options);
                return new EFCoreDbContextWrapper(other, NullLogger<EFCoreDbContextWrapper>.Instance);
            }

            var ctx = _serviceProvider.GetRequiredService<WriteGuardTestDbContext>();
            return new EFCoreDbContextWrapper(ctx, NullLogger<EFCoreDbContextWrapper>.Instance);
        }
    }

    public class WriteGuardTestDbContext : DbContext
    {
        public WriteGuardTestDbContext(DbContextOptions<WriteGuardTestDbContext> options) : base(options)
        {
        }

        public DbSet<WriteGuardEntity> Entities => Set<WriteGuardEntity>();
    }

    public class WriteGuardEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
