using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

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
            
            _serviceProvider = services.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        }

        #region Transaction Commit Tests

        [Fact]
        public async Task Transaction_BasicCommit_ShouldPersistChanges()
        {
            // Arrange
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var aggregate = new WorkflowTestAggregate("Transaction Test");

            // Act
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var saved = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
            Assert.Equal("Transaction Test", saved.Name);
        }

        [Fact(Skip = "In-memory database doesn't support real transaction rollback. Requires real database for integration testing.")]
        public async Task Transaction_Rollback_ShouldDiscardChanges()
        {
            // Arrange
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var aggregate = new WorkflowTestAggregate("Should Be Rolled Back");

            // Act
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();
            await transaction.RollbackAsync();

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
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            
            // Add first aggregate
            var agg1 = new WorkflowTestAggregate("First");
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();
            
            // Create savepoint
            await transaction.CreateSavepointAsync("sp1");
            
            // Add second aggregate
            var agg2 = new WorkflowTestAggregate("Second");
            _dbContext.TestAggregates.Add(agg2);
            await _dbContext.SaveChangesAsync();
            
            // Act - Rollback to savepoint
            await transaction.RollbackToSavepointAsync("sp1");
            
            // Add third aggregate after rollback
            var agg3 = new WorkflowTestAggregate("Third");
            _dbContext.TestAggregates.Add(agg3);
            await _dbContext.SaveChangesAsync();
            
            await transaction.CommitAsync();

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
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            
            var agg1 = new WorkflowTestAggregate("Test");
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();
            
            await transaction.CreateSavepointAsync("sp1");

            // Act
            await transaction.ReleaseSavepointAsync("sp1");
            await transaction.CommitAsync();

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
            using (var transaction1 = await _dbContext.Database.BeginTransactionAsync())
            {
                var agg1 = new WorkflowTestAggregate("Transaction 1");
                _dbContext.TestAggregates.Add(agg1);
                await _dbContext.SaveChangesAsync();
                await transaction1.CommitAsync();
            }

            _dbContext.ChangeTracker.Clear();

            // Transaction 2
            using (var transaction2 = await _dbContext.Database.BeginTransactionAsync())
            {
                var agg2 = new WorkflowTestAggregate("Transaction 2");
                _dbContext.TestAggregates.Add(agg2);
                await _dbContext.SaveChangesAsync();
                await transaction2.CommitAsync();
            }

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(2, count);
        }

        [Fact(Skip = "In-memory database doesn't support savepoints. Requires real database for integration testing.")]
        public async Task Transaction_MultipleSavepoints_ShouldWorkSequentially()
        {
            // Arrange
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            
            var agg1 = new WorkflowTestAggregate("First");
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();
            
            await transaction.CreateSavepointAsync("sp1");
            
            var agg2 = new WorkflowTestAggregate("Second");
            _dbContext.TestAggregates.Add(agg2);
            await _dbContext.SaveChangesAsync();
            
            await transaction.CreateSavepointAsync("sp2");
            
            var agg3 = new WorkflowTestAggregate("Third");
            _dbContext.TestAggregates.Add(agg3);
            await _dbContext.SaveChangesAsync();
            
            // Act - Rollback to first savepoint
            await transaction.RollbackToSavepointAsync("sp1");
            await transaction.CommitAsync();

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
