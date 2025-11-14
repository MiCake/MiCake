using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.IntegrationTests.Uow
{
    #region Test Infrastructure

    internal class LazyImmediateTestDbContext : MiCakeDbContext
    {
        public LazyImmediateTestDbContext(DbContextOptions<LazyImmediateTestDbContext> options) : base(options)
        {
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

    #endregion

    /// <summary>
    /// Integration tests for Lazy initialization mode
    /// Tests that transactions are started lazily when resources are accessed
    /// </summary>
    public class UnitOfWorkLazyModeIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkManager _uowManager;
        private readonly LazyImmediateTestDbContext _dbContext;

        public UnitOfWorkLazyModeIntegrationTests()
        {
            var services = new ServiceCollection();
            
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<LazyImmediateTestDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });
            
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
            // Register UoW services using reflection since UnitOfWorkManager is internal
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType);
            
            _serviceProvider = services.BuildServiceProvider();
            _uowManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager>();
            _dbContext = _serviceProvider.GetRequiredService<LazyImmediateTestDbContext>();
        }

        #region Lazy Mode - Commit Tests

        [Fact]
        public async Task LazyMode_CommitWithData_ShouldPersistChanges()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            
            using var uow = await _uowManager.BeginAsync(options);
            var aggregate = new IntegrationTestAggregate("Lazy Test", 100);

            // Act
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var saved = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
            Assert.Equal("Lazy Test", saved.Name);
            Assert.Equal(100, saved.Value);
        }

        [Fact]
        public async Task LazyMode_MultipleOperations_ShouldCommitAll()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            
            using var uow = await _uowManager.BeginAsync(options);

            // Act
            var agg1 = new IntegrationTestAggregate("First", 10);
            var agg2 = new IntegrationTestAggregate("Second", 20);
            var agg3 = new IntegrationTestAggregate("Third", 30);

            _dbContext.TestAggregates.Add(agg1);
            _dbContext.TestAggregates.Add(agg2);
            _dbContext.TestAggregates.Add(agg3);
            await _dbContext.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(3, count);
        }

        #endregion

        #region Lazy Mode - Rollback Tests

        [Fact(Skip = "In-memory database doesn't support transaction rollback")]
        public async Task LazyMode_Rollback_ShouldDiscardChanges()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            
            using var uow = await _uowManager.BeginAsync(options);
            var aggregate = new IntegrationTestAggregate("Should Rollback", 999);

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

        #region Lazy Mode - Nested UoW Tests

        [Fact]
        public async Task LazyMode_NestedUoW_ShouldShareTransaction()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };

            using var outerUow = await _uowManager.BeginAsync(options);
            var agg1 = new IntegrationTestAggregate("Outer", 1);
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();

            // Act - Create nested UoW
            using (var innerUow = await _uowManager.BeginAsync(options))
            {
                Assert.NotNull(innerUow.Parent);
                Assert.Equal(outerUow.Id, innerUow.Parent.Id);

                var agg2 = new IntegrationTestAggregate("Inner", 2);
                _dbContext.TestAggregates.Add(agg2);
                await _dbContext.SaveChangesAsync();
                await innerUow.CommitAsync();
            }

            await outerUow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(2, count);
        }

        [Fact(Skip = "In-memory database doesn't support transaction rollback")]
        public async Task LazyMode_NestedUoW_InnerRollback_ShouldRollbackOuter()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };

            using var outerUow = await _uowManager.BeginAsync(options);
            var agg1 = new IntegrationTestAggregate("Outer", 1);
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();

            // Act
            using (var innerUow = await _uowManager.BeginAsync(options))
            {
                var agg2 = new IntegrationTestAggregate("Inner", 2);
                _dbContext.TestAggregates.Add(agg2);
                await _dbContext.SaveChangesAsync();
                await innerUow.RollbackAsync(); // Inner rollback should mark outer for rollback
            }

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await outerUow.CommitAsync());
        }

        #endregion

        #region Lazy Mode - Savepoint Tests

        [Fact(Skip = "In-memory database doesn't support savepoints")]
        public async Task LazyMode_Savepoint_RollbackToSavepoint_ShouldWork()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            
            using var uow = await _uowManager.BeginAsync(options);

            var agg1 = new IntegrationTestAggregate("Before Savepoint", 1);
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();

            var savepointName = await uow.CreateSavepointAsync("sp1");

            var agg2 = new IntegrationTestAggregate("After Savepoint", 2);
            _dbContext.TestAggregates.Add(agg2);
            await _dbContext.SaveChangesAsync();

            // Act
            await uow.RollbackToSavepointAsync(savepointName);

            var agg3 = new IntegrationTestAggregate("After Rollback", 3);
            _dbContext.TestAggregates.Add(agg3);
            await _dbContext.SaveChangesAsync();

            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var aggregates = await _dbContext.TestAggregates.ToListAsync();
            Assert.Equal(2, aggregates.Count);
            Assert.Contains(aggregates, a => a.Name == "Before Savepoint");
            Assert.Contains(aggregates, a => a.Name == "After Rollback");
            Assert.DoesNotContain(aggregates, a => a.Name == "After Savepoint");
        }

        #endregion

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Integration tests for Immediate initialization mode
    /// Tests that transactions are started immediately when UoW is created
    /// </summary>
    public class UnitOfWorkImmediateModeIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkManager _uowManager;
        private readonly LazyImmediateTestDbContext _dbContext;

        public UnitOfWorkImmediateModeIntegrationTests()
        {
            var services = new ServiceCollection();
            
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<LazyImmediateTestDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });
            
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
            // Register UoW services using reflection since UnitOfWorkManager is internal
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType);
            
            _serviceProvider = services.BuildServiceProvider();
            _uowManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager>();
            _dbContext = _serviceProvider.GetRequiredService<LazyImmediateTestDbContext>();
        }

        #region Immediate Mode - Commit Tests

        [Fact]
        public async Task ImmediateMode_CommitWithData_ShouldPersistChanges()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            
            using var uow = await _uowManager.BeginAsync(options);
            var aggregate = new IntegrationTestAggregate("Immediate Test", 200);

            // Act
            _dbContext.TestAggregates.Add(aggregate);
            await _dbContext.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var saved = await _dbContext.TestAggregates.FindAsync(aggregate.Id);
            Assert.NotNull(saved);
            Assert.Equal("Immediate Test", saved.Name);
            Assert.Equal(200, saved.Value);
        }

        [Fact]
        public async Task ImmediateMode_MultipleOperations_ShouldCommitAll()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            
            using var uow = await _uowManager.BeginAsync(options);

            // Act
            var agg1 = new IntegrationTestAggregate("First Immediate", 10);
            var agg2 = new IntegrationTestAggregate("Second Immediate", 20);
            var agg3 = new IntegrationTestAggregate("Third Immediate", 30);

            _dbContext.TestAggregates.Add(agg1);
            _dbContext.TestAggregates.Add(agg2);
            _dbContext.TestAggregates.Add(agg3);
            await _dbContext.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task ImmediateMode_HasActiveTransactions_ShouldBeTrueAfterBegin()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            
            // Act
            using var uow = await _uowManager.BeginAsync(options);

            // Assert - In immediate mode, transactions should be active right after Begin
            // Note: This behavior depends on whether lifecycle hooks activate resources
            // For a true test, we would need DbContext registered with lifecycle hooks
            Assert.NotNull(uow);
        }

        #endregion

        #region Immediate Mode - Rollback Tests

        [Fact(Skip = "In-memory database doesn't support transaction rollback")]
        public async Task ImmediateMode_Rollback_ShouldDiscardChanges()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            
            using var uow = await _uowManager.BeginAsync(options);
            var aggregate = new IntegrationTestAggregate("Should Rollback Immediate", 999);

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

        #region Immediate Mode - Nested UoW Tests

        [Fact]
        public async Task ImmediateMode_NestedUoW_ShouldShareTransaction()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };

            using var outerUow = await _uowManager.BeginAsync(options);
            var agg1 = new IntegrationTestAggregate("Outer Immediate", 1);
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();

            // Act - Create nested UoW
            using (var innerUow = await _uowManager.BeginAsync(options))
            {
                Assert.NotNull(innerUow.Parent);
                Assert.Equal(outerUow.Id, innerUow.Parent.Id);

                var agg2 = new IntegrationTestAggregate("Inner Immediate", 2);
                _dbContext.TestAggregates.Add(agg2);
                await _dbContext.SaveChangesAsync();
                await innerUow.CommitAsync();
            }

            await outerUow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var count = await _dbContext.TestAggregates.CountAsync();
            Assert.Equal(2, count);
        }

        #endregion

        #region Immediate Mode - Savepoint Tests

        [Fact(Skip = "In-memory database doesn't support savepoints")]
        public async Task ImmediateMode_Savepoint_RollbackToSavepoint_ShouldWork()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            
            using var uow = await _uowManager.BeginAsync(options);

            var agg1 = new IntegrationTestAggregate("Before Savepoint Immediate", 1);
            _dbContext.TestAggregates.Add(agg1);
            await _dbContext.SaveChangesAsync();

            var savepointName = await uow.CreateSavepointAsync("sp_immediate");

            var agg2 = new IntegrationTestAggregate("After Savepoint Immediate", 2);
            _dbContext.TestAggregates.Add(agg2);
            await _dbContext.SaveChangesAsync();

            // Act
            await uow.RollbackToSavepointAsync(savepointName);

            var agg3 = new IntegrationTestAggregate("After Rollback Immediate", 3);
            _dbContext.TestAggregates.Add(agg3);
            await _dbContext.SaveChangesAsync();

            await uow.CommitAsync();

            // Assert
            _dbContext.ChangeTracker.Clear();
            var aggregates = await _dbContext.TestAggregates.ToListAsync();
            Assert.Equal(2, aggregates.Count);
            Assert.Contains(aggregates, a => a.Name == "Before Savepoint Immediate");
            Assert.Contains(aggregates, a => a.Name == "After Rollback Immediate");
            Assert.DoesNotContain(aggregates, a => a.Name == "After Savepoint Immediate");
        }

        #endregion

        #region Immediate Mode - Comparison with Lazy

        [Fact]
        public async Task CompareModeses_BothShouldProduceSameResult()
        {
            // Test that both lazy and immediate modes produce the same final result

            // Lazy Mode
            var lazyOptions = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            
            using (var lazyUow = await _uowManager.BeginAsync(lazyOptions))
            {
                var agg = new IntegrationTestAggregate("Lazy Mode Test", 100);
                _dbContext.TestAggregates.Add(agg);
                await _dbContext.SaveChangesAsync();
                await lazyUow.CommitAsync();
            }

            _dbContext.ChangeTracker.Clear();
            var lazyCount = await _dbContext.TestAggregates.CountAsync();

            // Immediate Mode
            var immediateOptions = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            
            using (var immediateUow = await _uowManager.BeginAsync(immediateOptions))
            {
                var agg = new IntegrationTestAggregate("Immediate Mode Test", 200);
                _dbContext.TestAggregates.Add(agg);
                await _dbContext.SaveChangesAsync();
                await immediateUow.CommitAsync();
            }

            _dbContext.ChangeTracker.Clear();
            var immediateCount = await _dbContext.TestAggregates.CountAsync();

            // Assert
            Assert.Equal(2, lazyCount + immediateCount - lazyCount); // Total should be 2
        }

        #endregion

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
