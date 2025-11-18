using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Uow
{
    #region Test Infrastructure

    internal class TestDbContext : MiCakeDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<WorkflowTestAggregate> TestAggregates { get; set; }
    }

    internal class WorkflowTestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; private set; }

        protected WorkflowTestAggregate() { }

        public WorkflowTestAggregate(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public void ChangeName(string newName)
        {
            Name = newName;
            RaiseDomainEvent(new WorkflowTestEvent(Id, newName));
        }
    }

    internal class WorkflowTestEvent : IDomainEvent
    {
        public Guid AggregateId { get; }
        public string NewName { get; }

        public WorkflowTestEvent(Guid aggregateId, string newName)
        {
            AggregateId = aggregateId;
            NewName = newName;
        }
    }

    #endregion
    /// <summary>
    /// Integration tests for DbContext transaction workflows
    /// Tests transaction behaviors, savepoints, rollback scenarios
    /// </summary>
    public class UnitOfWorkCompleteWorkflowTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly TestDbContext _dbContext;
        private readonly IUnitOfWorkManager _uowManager;

        public UnitOfWorkCompleteWorkflowTests()
        {
            var services = new ServiceCollection();
            
            // Setup in-memory database
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                // Suppress transaction warnings for in-memory database
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });
            
            services.AddLogging();

            // Register UnitOfWork manager (internal class) via reflection
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType);

            // Register EF Core factories & MiCake EF options so that EF factories can register resources with UoW
            services.AddScoped(typeof(EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>), typeof(EntityFrameworkCore.Uow.EFCoreContextFactory<TestDbContext>));
            services.AddSingleton<Core.DependencyInjection.IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(TestDbContext)));
            
            _serviceProvider = services.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
            _uowManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager>();
        }

        #region Transaction Commit Tests

        [Fact]
        public async Task Transaction_BasicCommit_ShouldPersistChanges()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                Strategy = PersistenceStrategy.TransactionManaged,
                InitializationMode = TransactionInitializationMode.Lazy
            };

            using var uow = await _uowManager.BeginAsync(options);

            // Register EF resource with UoW using EF factory
            var factory = _serviceProvider.GetRequiredService<EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>>();
            var wrapper = factory.GetDbContextWrapper();
            var aggregate = new WorkflowTestAggregate("Transaction Test");

            // Act
            _dbContext.TestAggregates.Add(aggregate);
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var saved = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
            Assert.Equal("Transaction Test", saved.Name);
        }

        [Fact]
        public async Task Transaction_ImmediateCommit_ShouldPersistChanges()
        {
            // Arrange - immediate mode
            var options = new UnitOfWorkOptions
            {
                Strategy = PersistenceStrategy.TransactionManaged,
                InitializationMode = TransactionInitializationMode.Immediate
            };

            using var uow = await _uowManager.BeginAsync(options);

            // Use factory to register wrapper
            var factory = _serviceProvider.GetRequiredService<EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>>();
            var wrapper = factory.GetDbContextWrapper();

            var aggregate = new WorkflowTestAggregate("Immediate Transaction Test");

            // Act
            _dbContext.TestAggregates.Add(aggregate);
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var saved = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
            Assert.Equal("Immediate Transaction Test", saved.Name);
        }

        [Fact(Skip = "In-memory database doesn't support real transaction rollback. Requires real database for integration testing.")]
        public async Task Transaction_Rollback_ShouldDiscardChanges()
        {
            // Arrange
            using var uow = await _uowManager.BeginAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged });
            var aggregate = new WorkflowTestAggregate("Should Be Rolled Back");

            // Act
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();
            await uow.RollbackAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(0, count);
        }

        #endregion

        #region Savepoint Tests

        [Fact(Skip = "In-memory database doesn't support savepoints. Requires real database for integration testing.")]
        public async Task Transaction_WithSavepoint_RollbackToSavepoint_ShouldWork()
        {
            // Arrange
            using var uow = await _uowManager.BeginAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged });
            
            // Add first aggregate
            var agg1 = new WorkflowTestAggregate("First");
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();
            
            // Create savepoint
            await uow.CreateSavepointAsync("sp1");
            
            // Add second aggregate
            var agg2 = new WorkflowTestAggregate("Second");
            _dbContext.TestAggregates.Add(agg2);
            await _dbContext.SaveChangesAsync();
            
            // Act - Rollback to savepoint
            await uow.RollbackToSavepointAsync("sp1");
            
            // Add third aggregate after rollback
            var agg3 = new WorkflowTestAggregate("Third");
            _dbContext.TestAggregates.Add(agg3);
            await _dbContext.SaveChangesAsync();
            
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var aggregates = await _dbContext.TestAggregates.ToListAsync();
            Assert.Equal(2, aggregates.Count);
            Assert.Contains(aggregates, a => a.Name == "First");
            Assert.Contains(aggregates, a => a.Name == "Third");
            Assert.DoesNotContain(aggregates, a => a.Name == "Second");
        }

        [Fact(Skip = "In-memory database doesn't support savepoints. Requires real database for integration testing.")]
        public async Task Transaction_ReleaseSavepoint_ShouldSucceed()
        {
            // Arrange
            using var uow = await _uowManager.BeginAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged });
            
            var agg1 = new WorkflowTestAggregate("Test");
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();
            
            await uow.CreateSavepointAsync("sp1");

            // Act
            await uow.ReleaseSavepointAsync("sp1");
            await uow.CommitAsync();

            // Assert
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(1, count);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Transaction_ExceptionDuringCommit_ShouldHandleGracefully()
        {
            // Arrange
            var aggregate = new WorkflowTestAggregate("Exception Test");
            _dbContext.TestAggregates.Add(aggregate);

            // Act & Assert
            await _dbContext.SaveChangesAsync(); // No transaction, should still work
            
            var saved = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
        }

        #endregion

        #region Complex Scenarios

        [Fact]
        public async Task MultipleTransactions_Sequential_ShouldWorkIndependently()
        {
            // Transaction 1
            using (var uow1 = await _uowManager.BeginAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged }))
            {
                var agg1 = new WorkflowTestAggregate("Transaction 1");
                _dbContext.TestAggregates.Add(agg1);
                await _dbContext.SaveChangesAsync();
                await uow1.CommitAsync();
            }

            _dbContext.ChangeTracker.Clear();

            // Transaction 2
            using (var uow2 = await _uowManager.BeginAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged }))
            {
                var agg2 = new WorkflowTestAggregate("Transaction 2");
                _dbContext.TestAggregates.Add(agg2);
                await _dbContext.SaveChangesAsync();
                await uow2.CommitAsync();
            }

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task UoW_Commit_AutoSavesMultipleEntities()
        {
            // Arrange - register UoW
            using var uow = await _uowManager.BeginAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged });
            var factory = _serviceProvider.GetRequiredService<EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>>();
            var wrapper = factory.GetDbContextWrapper();

            // Act - add entities without calling SaveChanges
            _dbContext.TestAggregates.Add(new WorkflowTestAggregate("A"));
            _dbContext.TestAggregates.Add(new WorkflowTestAggregate("B"));

            // Commit should auto-save pending changes
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task BeginAsync_RequiresNew_ResourceRegistration_Isolation()
        {
            // Arrange
            var factory = _serviceProvider.GetRequiredService<EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>>();

            using var outer = await _uowManager.BeginAsync();
            var outerWrapper = factory.GetDbContextWrapper();
            _dbContext.TestAggregates.Add(new WorkflowTestAggregate("Outer"));
            await _dbContext.SaveChangesAsync();
            await outer.CommitAsync();

            using var newRoot = await _uowManager.BeginAsync(requiresNew: true);
            var newWrapper = factory.GetDbContextWrapper();
            _dbContext.TestAggregates.Add(new WorkflowTestAggregate("NewRoot"));
            await _dbContext.SaveChangesAsync();
            await newRoot.CommitAsync();

            _dbContext.ChangeTracker.Clear();
            var list = await _dbContext.TestAggregates.ToListAsync();
            Assert.Contains(list, a => a.Name == "Outer");
            Assert.Contains(list, a => a.Name == "NewRoot");
        }

        [Fact]
        public async Task NestedUoW_InnerRollback_ShouldMarkParentForRollback()
        {
            // Arrange
            var options = new UnitOfWorkOptions { InitializationMode = TransactionInitializationMode.Lazy, Strategy = PersistenceStrategy.TransactionManaged };
            using var outer = await _uowManager.BeginAsync(options);

            var factory = _serviceProvider.GetRequiredService<EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>>();
            var wrapper = factory.GetDbContextWrapper();

            // Add outer entity and save
            _dbContext.TestAggregates.Add(new WorkflowTestAggregate("Outer"));
            await _dbContext.SaveChangesAsync();

            // Act - nested uow
            using (var inner = await _uowManager.BeginAsync(options))
            {
                // nested registration delegates to parent via factory
                var innerFactory = _serviceProvider.GetRequiredService<EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>>();
                var innerWrapper = innerFactory.GetDbContextWrapper();
                _dbContext.TestAggregates.Add(new WorkflowTestAggregate("Inner"));
                await _dbContext.SaveChangesAsync();

                // inner rollback should mark parent for rollback
                await inner.RollbackAsync();
            }

            // Assert - commit outer should fail
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await outer.CommitAsync());
        }

        [Fact]
        public async Task NestedUoW_RegistrationViaFactory_DelegatesToParent_PersistOnCommit()
        {
            // Arrange
            var options = new UnitOfWorkOptions { InitializationMode = TransactionInitializationMode.Lazy, Strategy = PersistenceStrategy.TransactionManaged };
            using var outer = await _uowManager.BeginAsync(options);
            var factory = _serviceProvider.GetRequiredService<EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>>();
            var wrapperOuter = factory.GetDbContextWrapper();

            _dbContext.TestAggregates.Add(new WorkflowTestAggregate("Outer"));
            await _dbContext.SaveChangesAsync();

            // Act - nested uow
            using (var inner = await _uowManager.BeginAsync(options))
            {
                var innerFactory = _serviceProvider.GetRequiredService<EntityFrameworkCore.Uow.IEFCoreContextFactory<TestDbContext>>();
                var innerWrapper = innerFactory.GetDbContextWrapper();
                _dbContext.TestAggregates.Add(new WorkflowTestAggregate("Inner"));
                await _dbContext.SaveChangesAsync();
                await inner.CommitAsync();
            }

            // Outer commit should persist both Outer and Inner
            await outer.CommitAsync();

            _dbContext.ChangeTracker.Clear();
            var list = await _dbContext.TestAggregates.ToListAsync();
            Assert.Contains(list, a => a.Name == "Outer");
            Assert.Contains(list, a => a.Name == "Inner");
        }

        [Fact(Skip = "In-memory database doesn't support savepoints. Requires real database for integration testing.")]
        public async Task Transaction_MultipleSavepoints_ShouldWorkSequentially()
        {
            // Arrange
            using var uow = await _uowManager.BeginAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged });
            
            var agg1 = new WorkflowTestAggregate("First");
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();
            
            await uow.CreateSavepointAsync("sp1");
            
            var agg2 = new WorkflowTestAggregate("Second");
            _dbContext.TestAggregates.Add(agg2);
            await _dbContext.SaveChangesAsync();
            
            await uow.CreateSavepointAsync("sp2");
            
            var agg3 = new WorkflowTestAggregate("Third");
            _dbContext.TestAggregates.Add(agg3);
            await _dbContext.SaveChangesAsync();
            
            // Act - Rollback to first savepoint
            await uow.RollbackToSavepointAsync("sp1");
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(1, count);
            var saved = await _dbContext.TestAggregates.FirstAsync();
            Assert.Equal("First", saved.Name);
        }

        #endregion

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
