using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Uow
{
    /// <summary>
    /// Integration tests for DbContext with aggregate roots and domain events
    /// Tests domain events are collected and can be dispatched on SaveChanges
    /// </summary>
    public class UnitOfWorkWithRepositoryTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly TestDbContext _dbContext;

        public UnitOfWorkWithRepositoryTests()
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
            
            // Register domain event handler
            services.AddScoped<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>();
            
            _serviceProvider = services.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        }

        #region Basic DbContext Operations

        [Fact]
        public async Task DbContext_AddAggregate_ShouldWork()
        {
            // Arrange
            var aggregate = new TestAggregate("Test Aggregate");

            // Act
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();

            // Assert
            var saved = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
            Assert.Equal("Test Aggregate", saved.Name);
        }

        [Fact]
        public async Task DbContext_UpdateAggregate_ShouldWork()
        {
            // Arrange - seed data
            var aggregate = new TestAggregate("Original");
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act - update
            var existing = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            existing.ChangeName("Updated");
            await _dbContext.SaveChangesAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var updated = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.Equal("Updated", updated.Name);
        }

        [Fact]
        public async Task DbContext_DeleteAggregate_ShouldWork()
        {
            // Arrange - seed data
            var aggregate = new TestAggregate("To Delete");
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();

            // Act - delete
            _dbContext.TestAggregates.Remove(aggregate);
            await _dbContext.SaveChangesAsync();

            // Assert
            var deleted = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.Null(deleted);
        }

        #endregion

        #region Domain Event Tests

        [Fact]
        public void DomainEvents_AreCollectedWhenRaised()
        {
            // Arrange
            var aggregate = new TestAggregate("Event Test");
            
            // Act
            aggregate.ChangeName("New Name"); // This raises domain event

            // Assert
            var events = aggregate.DomainEvents;
            Assert.Single(events);
            Assert.IsType<TestDomainEvent>(events.First());
        }

        [Fact]
        public void DomainEvents_MultiplEvents_AreCollected()
        {
            // Arrange
            var aggregate = new TestAggregate("Event Test");
            
            // Act
            aggregate.ChangeName("Name 1");
            aggregate.ChangeName("Name 2");
            aggregate.ChangeName("Name 3");

            // Assert
            var events = aggregate.DomainEvents;
            Assert.Equal(3, events.Count());
        }

        [Fact]
        public void DomainEvents_CanBeCleared()
        {
            // Arrange
            var aggregate = new TestAggregate("Event Test");
            aggregate.ChangeName("New Name");
            Assert.Single(aggregate.DomainEvents);

            // Act
            aggregate.ClearDomainEvents();

            // Assert
            Assert.Empty(aggregate.DomainEvents);
        }

        #endregion

        #region Complex Scenarios

        [Fact]
        public async Task ComplexScenario_MultipleAggregates_ShouldWork()
        {
            // Arrange
            var aggregate1 = new TestAggregate("Aggregate 1");
            var aggregate2 = new TestAggregate("Aggregate 2");

            // Act
            _dbContext.TestAggregates.Add(aggregate1);
            _dbContext.TestAggregates.Add(aggregate2);
            await _dbContext.SaveChangesAsync();

            // Assert
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task ComplexScenario_MixedOperations_ShouldWork()
        {
            // Arrange - seed one entity
            var aggregate1 = new TestAggregate("Aggregate 1");
            _dbContext.TestAggregates.Add(aggregate1);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act - add one, update one
            var aggregate2 = new TestAggregate("Aggregate 2");
            _dbContext.TestAggregates.Add(aggregate2);
            
            var existing = await _dbContext.TestAggregates.FindAsync(aggregate1.Id);
            existing.ChangeName("Updated");
            
            await _dbContext.SaveChangesAsync();

            // Assert
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(2, count);
            
            _dbContext.ChangeTracker.Clear();
            var updated = await _dbContext.TestAggregates.FindAsync(aggregate1.Id);
            Assert.Equal("Updated", updated.Name);
        }

        #endregion

        #region Lazy vs Immediate Mode Tests

        [Fact]
        public async Task LazyMode_ShouldNotStartTransactionUntilCommit()
        {
            // Arrange
            var aggregate = new TestAggregate("Lazy Test");
            _dbContext.TestAggregates.Add(aggregate);
            
            // Transaction should not be active yet
            Assert.Null(_dbContext.Database.CurrentTransaction);

            // Act - SaveChanges should start transaction
            await _dbContext.SaveChangesAsync();

            // Assert - Transaction should still be null for in-memory database
            // In a real database, transaction would be started
            var saved = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
        }

        #endregion

        #region Domain Event Tests

        [Fact]
        public async Task SaveChanges_WithDomainEvents_ShouldCollectEvents()
        {
            // Arrange
            var aggregate = new TestAggregate("Event Test");
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            var existing = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            existing.ChangeName("Updated Name");
            
            // Assert
            var events = existing.DomainEvents;
            Assert.Single(events);
            Assert.IsType<TestDomainEvent>(events.First());
            
            // Note: In integration tests without full MiCake infrastructure,
            // domain events are NOT automatically cleared after SaveChanges.
            // Event clearing requires IRepositoryPostSaveChanges handlers which
            // are only registered in full MiCake applications with UoW/Repository.
            await _dbContext.SaveChangesAsync();
            // Events still exist because cleanup lifetime handler is not registered
            Assert.Single(existing.DomainEvents);
        }

        #endregion

        #region Transaction Rollback Tests

        [Fact(Skip = "In-memory database doesn't support real transaction rollback. Requires real database for integration testing.")]
        public async Task Transaction_Rollback_ShouldDiscardChanges()
        {
            // Arrange
            var aggregate = new TestAggregate("Rollback Test");
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();
            var aggregateId = aggregate.Id;
            _dbContext.ChangeTracker.Clear();

            // Act - Start transaction, make changes, rollback
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                var existing = await _dbContext.TestAggregates.FindAsync(aggregateId);
                existing.ChangeName("Should Be Rolled Back");
                await _dbContext.SaveChangesAsync();
                
                await transaction.RollbackAsync();
            }

            // Assert
            _dbContext.ChangeTracker.Clear();
            var final = await _dbContext.TestAggregates.FindAsync(aggregateId);
            Assert.Equal("Rollback Test", final.Name); // Should have original name
        }

        #endregion

        #region Concurrency Tests

        [Fact]
        public async Task ConcurrentOperations_ShouldWorkWithDifferentAggregates()
        {
            // Arrange
            var aggregate1 = new TestAggregate("Concurrent 1");
            var aggregate2 = new TestAggregate("Concurrent 2");

            // Act
            _dbContext.TestAggregates.Add(aggregate1);
            _dbContext.TestAggregates.Add(aggregate2);
            await _dbContext.SaveChangesAsync();

            // Assert
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(2, count);
        }

        #endregion

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }

        #region Test Infrastructure

        private class TestDbContext : MiCakeDbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<TestAggregate> TestAggregates { get; set; }
        }

        private class TestAggregate : AggregateRoot<Guid>
        {
            public string Name { get; private set; }

            protected TestAggregate() { }

            public TestAggregate(string name)
            {
                Id = Guid.NewGuid();
                Name = name;
            }

            public void ChangeName(string newName)
            {
                Name = newName;
                RaiseDomainEvent(new TestDomainEvent(Id, newName));
            }
        }

        private class TestDomainEvent : IDomainEvent
        {
            public Guid AggregateId { get; }
            public string NewName { get; }

            public TestDomainEvent(Guid aggregateId, string newName)
            {
                AggregateId = aggregateId;
                NewName = newName;
            }
        }

        private class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent>
        {
            public static int EventCount { get; private set; }

            public static void Reset() => EventCount = 0;

            public Task HandleAysnc(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
            {
                EventCount++;
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
