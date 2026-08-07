using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Uow
{
    #region Test Infrastructure

    internal class LazyImmediateTestDbContext : MiCakeDbContext
    {
        public LazyImmediateTestDbContext(DbContextOptions<LazyImmediateTestDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // WORKAROUND for defect found during t7 acceptance testing:
            // MiCakeDbContext.OnConfiguring installs a provider-less fallback write-guard
            // interceptor that conflicts with UseMiCakeInterceptors(sp); skip the base call
            // until the defect is fixed (see test-design.md).
        }

        public DbSet<IntegrationTestAggregate> TestAggregates { get; set; }
    }

    internal class IntegrationTestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; private set; }
        public int Value { get; private set; }

        protected IntegrationTestAggregate() { }

        public IntegrationTestAggregate(string name, int value)
        {
            Id = Guid.NewGuid();
            Name = name;
            Value = value;
        }

        public void UpdateValue(int newValue)
        {
            Value = newValue;
            RaiseDomainEvent(new IntegrationTestEvent(Id, newValue));
        }
    }

    internal class IntegrationTestEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public int NewValue { get; }

        public IntegrationTestEvent(Guid aggregateId, int newValue)
        {
            AggregateId = aggregateId;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Builds a file-backed SQLite provider with the full MiCake UoW write pipeline for the
    /// lazy/immediate initialization-mode tests. Supersedes the previous InMemory wiring, which
    /// could not exercise transaction rollback or savepoints (the skipped tests now run for real).
    /// </summary>
    internal sealed class SqliteModeTestHost : IDisposable
    {
        private readonly string _dbPath;

        public ServiceProvider Provider { get; }

        public SqliteModeTestHost()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"micake-uow-mode-{Guid.NewGuid():N}.db");

            var services = new ServiceCollection();

            services.AddDbContext<LazyImmediateTestDbContext>((sp, opt) =>
                opt.UseSqlite($"Data Source={_dbPath};Pooling=False"));
            services.ConfigureDbContext<LazyImmediateTestDbContext>((sp, builder) =>
                builder.AddInterceptors(
                    sp.GetRequiredService<MiCakeEFCoreInterceptor>(),
                    sp.GetRequiredService<MiCakeDbCommandInterceptor>()));

            services.AddLogging();
            services.AddUowCoreServices(typeof(LazyImmediateTestDbContext));

            services.AddSingleton<AmbientUnitOfWorkAccessor>();
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => sp.GetRequiredService<AmbientUnitOfWorkAccessor>());
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(LazyImmediateTestDbContext)));
            services.AddSingleton<MiCakeEFCoreInterceptor>();
            services.AddSingleton<MiCakeDbCommandInterceptor>();

            Provider = services.BuildServiceProvider();
            Provider.GetRequiredService<IDbContextTypeRegistry>().RegisterDbContextType(typeof(LazyImmediateTestDbContext));

            using var scope = Provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<LazyImmediateTestDbContext>().Database.EnsureCreated();
        }

        /// <summary>
        /// Resolves the frame-stable DbContext for the current unit of work.
        /// </summary>
        public static LazyImmediateTestDbContext GetContext(IServiceProvider serviceProvider)
            => serviceProvider.GetRequiredService<IEFCoreContextFactory<LazyImmediateTestDbContext>>().GetDbContext();

        /// <summary>
        /// Counts aggregates outside any unit of work (direct DI resolution).
        /// </summary>
        public async Task<int> CountAggregatesAsync()
        {
            await using var scope = Provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LazyImmediateTestDbContext>();
            return await context.TestAggregates.CountAsync();
        }

        public void Dispose()
        {
            Provider.Dispose();
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch (IOException)
            {
                // A lingering connection may still hold the file; the OS temp cleaner handles it.
            }
        }
    }

    #endregion

    /// <summary>
    /// Integration tests for Lazy initialization mode on a relational store.
    /// Transactions are started lazily when resources are first written.
    /// </summary>
    public class UnitOfWorkLazyModeIntegrationTests : IDisposable
    {
        private readonly SqliteModeTestHost _host = new();

        private static UnitOfWorkOptions LazyOptions() => new()
        {
            InitializationMode = TransactionInitializationMode.Lazy
        };

        #region Lazy Mode - Commit Tests

        [Fact]
        public async Task LazyMode_CommitWithData_ShouldPersistChanges()
        {
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(LazyOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            var aggregate = new IntegrationTestAggregate("Lazy Test", 100);

            // Act
            dbContext.TestAggregates.Add(aggregate);
            await dbContext.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            Assert.Equal(1, await _host.CountAggregatesAsync());
            await using var verifyScope = _host.Provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<LazyImmediateTestDbContext>();
            var saved = await verifyContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
            Assert.Equal("Lazy Test", saved.Name);
            Assert.Equal(100, saved.Value);
        }

        [Fact]
        public async Task LazyMode_MultipleOperations_ShouldCommitAll()
        {
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(LazyOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            // Act
            dbContext.TestAggregates.Add(new IntegrationTestAggregate("First", 10));
            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Second", 20));
            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Third", 30));
            await dbContext.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            Assert.Equal(3, await _host.CountAggregatesAsync());
        }

        #endregion

        #region Lazy Mode - Rollback Tests

        [Fact]
        public async Task LazyMode_Rollback_ShouldDiscardChanges()
        {
            // Supersedes the skipped InMemory version: on file-backed SQLite the flushed
            // write sits inside the UoW transaction and rollback discards it durably.
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(LazyOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            var aggregate = new IntegrationTestAggregate("Should Rollback", 999);

            // Act
            dbContext.TestAggregates.Add(aggregate);
            await dbContext.SaveChangesAsync();
            await uow.RollbackAsync();

            // Assert
            Assert.Equal(0, await _host.CountAggregatesAsync());
        }

        #endregion

        #region Lazy Mode - Nested UoW Tests

        [Fact]
        public async Task LazyMode_NestedUoW_ShouldShareTransaction()
        {
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var outerUow = await uowManager.BeginAsync(LazyOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Outer", 1));
            await dbContext.SaveChangesAsync();

            // Act - Create nested UoW
            await using (var innerUow = await uowManager.BeginAsync(LazyOptions()))
            {
                Assert.NotNull(innerUow.Parent);
                Assert.Equal(outerUow.Id, innerUow.Parent.Id);

                dbContext.TestAggregates.Add(new IntegrationTestAggregate("Inner", 2));
                await dbContext.SaveChangesAsync();
                await innerUow.CommitAsync();
            }

            // The nested write is not visible before the root commits.
            Assert.Equal(0, await _host.CountAggregatesAsync());

            await outerUow.CommitAsync();

            // Assert
            Assert.Equal(2, await _host.CountAggregatesAsync());
        }

        [Fact]
        public async Task LazyMode_NestedUoW_InnerRollback_ShouldRollbackOuter()
        {
            // Supersedes the skipped InMemory version: a shared nested rollback marks the
            // root rollback-only, so the root commit is rejected and nothing persists.
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var outerUow = await uowManager.BeginAsync(LazyOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Outer", 1));
            await dbContext.SaveChangesAsync();

            // Act
            await using (var innerUow = await uowManager.BeginAsync(LazyOptions()))
            {
                dbContext.TestAggregates.Add(new IntegrationTestAggregate("Inner", 2));
                await dbContext.SaveChangesAsync();
                await innerUow.RollbackAsync(); // Inner rollback marks the root rollback-only
            }

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await outerUow.CommitAsync());
            await outerUow.RollbackAsync();

            Assert.Equal(0, await _host.CountAggregatesAsync());
        }

        #endregion

        #region Lazy Mode - Savepoint Tests

        [Fact]
        public async Task LazyMode_Savepoint_RollbackToSavepoint_ShouldWork()
        {
            // Supersedes the skipped InMemory version on file-backed SQLite.
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(LazyOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Before Savepoint", 1));
            await dbContext.SaveChangesAsync();

            var savepointName = await uow.CreateSavepointAsync("sp1");

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("After Savepoint", 2));
            await dbContext.SaveChangesAsync();

            // Act
            await uow.RollbackToSavepointAsync(savepointName);

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("After Rollback", 3));
            await dbContext.SaveChangesAsync();

            await uow.CommitAsync();

            // Assert
            await using var verifyScope = _host.Provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<LazyImmediateTestDbContext>();
            var aggregates = await verifyContext.TestAggregates.ToListAsync();
            Assert.Equal(2, aggregates.Count);
            Assert.Contains(aggregates, a => a.Name == "Before Savepoint");
            Assert.Contains(aggregates, a => a.Name == "After Rollback");
            Assert.DoesNotContain(aggregates, a => a.Name == "After Savepoint");
        }

        #endregion

        public void Dispose()
        {
            _host.Dispose();
        }
    }

    /// <summary>
    /// Integration tests for Immediate initialization mode on a relational store.
    /// Transactions are activated for every registered resource when the UoW begins.
    /// </summary>
    public class UnitOfWorkImmediateModeIntegrationTests : IDisposable
    {
        private readonly SqliteModeTestHost _host = new();

        private static UnitOfWorkOptions ImmediateOptions() => new()
        {
            InitializationMode = TransactionInitializationMode.Immediate
        };

        private static UnitOfWorkOptions LazyOptions() => new()
        {
            InitializationMode = TransactionInitializationMode.Lazy
        };

        #region Immediate Mode - Commit Tests

        [Fact]
        public async Task ImmediateMode_CommitWithData_ShouldPersistChanges()
        {
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(ImmediateOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            var aggregate = new IntegrationTestAggregate("Immediate Test", 200);

            // Act
            dbContext.TestAggregates.Add(aggregate);
            await dbContext.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            Assert.Equal(1, await _host.CountAggregatesAsync());
            await using var verifyScope = _host.Provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<LazyImmediateTestDbContext>();
            var saved = await verifyContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
            Assert.Equal("Immediate Test", saved.Name);
            Assert.Equal(200, saved.Value);
        }

        [Fact]
        public async Task ImmediateMode_MultipleOperations_ShouldCommitAll()
        {
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(ImmediateOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            // Act
            dbContext.TestAggregates.Add(new IntegrationTestAggregate("First Immediate", 10));
            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Second Immediate", 20));
            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Third Immediate", 30));
            await dbContext.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            Assert.Equal(3, await _host.CountAggregatesAsync());
        }

        [Fact]
        public async Task ImmediateMode_HasActiveTransactions_ShouldBeTrueAfterBegin()
        {
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            // Act
            await using var uow = await uowManager.BeginAsync(ImmediateOptions());

            // Assert - immediate initialization activates the registered DbContext resource
            // transaction when the unit of work begins, before any application write.
            Assert.True(uow.HasActiveTransactions);

            await uow.RollbackAsync();
        }

        #endregion

        #region Immediate Mode - Rollback Tests

        [Fact]
        public async Task ImmediateMode_Rollback_ShouldDiscardChanges()
        {
            // Supersedes the skipped InMemory version on file-backed SQLite.
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(ImmediateOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            var aggregate = new IntegrationTestAggregate("Should Rollback Immediate", 999);

            // Act
            dbContext.TestAggregates.Add(aggregate);
            await dbContext.SaveChangesAsync();
            await uow.RollbackAsync();

            // Assert
            Assert.Equal(0, await _host.CountAggregatesAsync());
        }

        #endregion

        #region Immediate Mode - Nested UoW Tests

        [Fact]
        public async Task ImmediateMode_NestedUoW_ShouldShareTransaction()
        {
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var outerUow = await uowManager.BeginAsync(ImmediateOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Outer Immediate", 1));
            await dbContext.SaveChangesAsync();

            // Act - Create nested UoW
            await using (var innerUow = await uowManager.BeginAsync(ImmediateOptions()))
            {
                Assert.NotNull(innerUow.Parent);
                Assert.Equal(outerUow.Id, innerUow.Parent.Id);

                dbContext.TestAggregates.Add(new IntegrationTestAggregate("Inner Immediate", 2));
                await dbContext.SaveChangesAsync();
                await innerUow.CommitAsync();
            }

            await outerUow.CommitAsync();

            // Assert
            Assert.Equal(2, await _host.CountAggregatesAsync());
        }

        #endregion

        #region Immediate Mode - Savepoint Tests

        [Fact]
        public async Task ImmediateMode_Savepoint_RollbackToSavepoint_ShouldWork()
        {
            // Supersedes the skipped InMemory version on file-backed SQLite.
            // Arrange
            await using var scope = _host.Provider.CreateAsyncScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(ImmediateOptions());
            var dbContext = SqliteModeTestHost.GetContext(scope.ServiceProvider);

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("Before Savepoint Immediate", 1));
            await dbContext.SaveChangesAsync();

            var savepointName = await uow.CreateSavepointAsync("sp_immediate");

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("After Savepoint Immediate", 2));
            await dbContext.SaveChangesAsync();

            // Act
            await uow.RollbackToSavepointAsync(savepointName);

            dbContext.TestAggregates.Add(new IntegrationTestAggregate("After Rollback Immediate", 3));
            await dbContext.SaveChangesAsync();

            await uow.CommitAsync();

            // Assert
            await using var verifyScope = _host.Provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<LazyImmediateTestDbContext>();
            var aggregates = await verifyContext.TestAggregates.ToListAsync();
            Assert.Equal(2, aggregates.Count);
            Assert.Contains(aggregates, a => a.Name == "Before Savepoint Immediate");
            Assert.Contains(aggregates, a => a.Name == "After Rollback Immediate");
            Assert.DoesNotContain(aggregates, a => a.Name == "After Savepoint Immediate");
        }

        #endregion

        #region Immediate Mode - Comparison with Lazy

        [Fact]
        public async Task CompareModes_BothShouldProduceSameResult()
        {
            // Test that both lazy and immediate modes produce the same final result.

            // Lazy Mode
            await using (var lazyScope = _host.Provider.CreateAsyncScope())
            {
                var uowManager = lazyScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var lazyUow = await uowManager.BeginAsync(LazyOptions());
                var dbContext = SqliteModeTestHost.GetContext(lazyScope.ServiceProvider);

                dbContext.TestAggregates.Add(new IntegrationTestAggregate("Lazy Mode Test", 100));
                await dbContext.SaveChangesAsync();
                await lazyUow.CommitAsync();
            }

            // Immediate Mode
            await using (var immediateScope = _host.Provider.CreateAsyncScope())
            {
                var uowManager = immediateScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var immediateUow = await uowManager.BeginAsync(ImmediateOptions());
                var dbContext = SqliteModeTestHost.GetContext(immediateScope.ServiceProvider);

                dbContext.TestAggregates.Add(new IntegrationTestAggregate("Immediate Mode Test", 200));
                await dbContext.SaveChangesAsync();
                await immediateUow.CommitAsync();
            }

            // Assert
            Assert.Equal(2, await _host.CountAggregatesAsync());
            await using var verifyScope = _host.Provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<LazyImmediateTestDbContext>();
            var names = await verifyContext.TestAggregates.Select(a => a.Name).ToListAsync();
            Assert.Contains("Lazy Mode Test", names);
            Assert.Contains("Immediate Mode Test", names);
        }

        #endregion

        public void Dispose()
        {
            _host.Dispose();
        }
    }
}
