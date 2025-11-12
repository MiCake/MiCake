using MiCake.Core;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace MiCake.IntegrationTests.EntityFrameworkCore
{
    /// <summary>
    /// Integration tests for Unit of Work functionality with EF Core
    /// Tests the complete UoW workflow including transaction management, nesting, savepoints, and rollback
    /// </summary>
    public class UnitOfWorkIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkManager _uowManager;

        public UnitOfWorkIntegrationTests()
        {
            var services = new ServiceCollection();

            // Configure in-memory database
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

            // Add core services
            services.AddLogging();

            // Add EF Core UoW services
            var builder = new DefaultMiCakeBuilderProvider(
                services, 
                typeof(MiCakeEssentialModule),
                new MiCakeApplicationOptions())
                .GetMiCakeBuilder();

            builder.UseEFCore<TestDbContext>(
                conventionBuilder: null,
                optionsBuilder: options =>
                {
                    options.ImplicitModeForUow = false; // Explicit UoW mode for testing
                    options.WillOpenTransactionForUow = true; // Enable transactions
                });

            builder.Build();

            _serviceProvider = services.BuildServiceProvider();
            _uowManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        public async Task UoW_Basic_CreateAndCommit_ShouldPersistChanges()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-001",
                CustomerName = "John Doe",
                TotalAmount = 100.50m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            using (var uow = _uowManager.Begin())
            {
                var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
                dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Assert
            var dbContext2 = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var savedOrder = await dbContext2.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-001");
            Assert.NotNull(savedOrder);
            Assert.Equal("John Doe", savedOrder.CustomerName);
            Assert.Equal(100.50m, savedOrder.TotalAmount);
        }

        [Fact]
        public async Task UoW_Rollback_ShouldNotPersistChanges()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-002",
                CustomerName = "Jane Smith",
                TotalAmount = 200.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            using (var uow = _uowManager.Begin())
            {
                var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
                dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync();
                await uow.RollbackAsync();
            }

            // Assert
            var dbContext2 = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var savedOrder = await dbContext2.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-002");
            Assert.Null(savedOrder);
        }

        [Fact]
        public async Task UoW_Nested_InnerCommit_OuterRollback_ShouldRollbackAll()
        {
            // Arrange
            var order1 = new Order
            {
                OrderNumber = "ORD-003",
                CustomerName = "Alice",
                TotalAmount = 150.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            var order2 = new Order
            {
                OrderNumber = "ORD-004",
                CustomerName = "Bob",
                TotalAmount = 250.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            using (var outerUow = _uowManager.Begin())
            {
                var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
                dbContext.Orders.Add(order1);
                await dbContext.SaveChangesAsync();

                using (var innerUow = _uowManager.Begin())
                {
                    dbContext.Orders.Add(order2);
                    await dbContext.SaveChangesAsync();
                    await innerUow.CommitAsync();
                }

                // Rollback outer UoW
                await outerUow.RollbackAsync();
            }

            // Assert - both orders should not be persisted
            var dbContext2 = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var savedOrder1 = await dbContext2.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-003");
            var savedOrder2 = await dbContext2.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-004");
            Assert.Null(savedOrder1);
            Assert.Null(savedOrder2);
        }

        [Fact]
        public async Task UoW_Nested_BothCommit_ShouldPersistAll()
        {
            // Arrange
            var order1 = new Order
            {
                OrderNumber = "ORD-005",
                CustomerName = "Charlie",
                TotalAmount = 175.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            var order2 = new Order
            {
                OrderNumber = "ORD-006",
                CustomerName = "David",
                TotalAmount = 275.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            using (var outerUow = _uowManager.Begin())
            {
                var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
                dbContext.Orders.Add(order1);
                await dbContext.SaveChangesAsync();

                using (var innerUow = _uowManager.Begin())
                {
                    dbContext.Orders.Add(order2);
                    await dbContext.SaveChangesAsync();
                    await innerUow.CommitAsync();
                }

                await outerUow.CommitAsync();
            }

            // Assert - both orders should be persisted
            var dbContext2 = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var savedOrder1 = await dbContext2.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-005");
            var savedOrder2 = await dbContext2.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-006");
            Assert.NotNull(savedOrder1);
            Assert.NotNull(savedOrder2);
            Assert.Equal("Charlie", savedOrder1.CustomerName);
            Assert.Equal("David", savedOrder2.CustomerName);
        }

        [Fact]
        public async Task UoW_WithoutCommit_ShouldNotPersist()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-007",
                CustomerName = "Eve",
                TotalAmount = 300.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Act - Create UoW but don't commit
            using (var uow = _uowManager.Begin())
            {
                var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
                dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync();
                // No commit or rollback - UoW disposed without action
            }

            // Assert
            var dbContext2 = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var savedOrder = await dbContext2.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-007");
            Assert.Null(savedOrder);
        }

        [Fact]
        public void UoW_Current_ShouldTrackActiveUoW()
        {
            // Arrange & Act & Assert
            Assert.Null(_uowManager.Current);

            using (var uow = _uowManager.Begin())
            {
                Assert.NotNull(_uowManager.Current);
                Assert.Equal(uow.Id, _uowManager.Current.Id);

                using (var innerUow = _uowManager.Begin())
                {
                    Assert.NotNull(_uowManager.Current);
                    Assert.Equal(innerUow.Id, _uowManager.Current.Id);
                }

                // After inner UoW disposed, current should be outer
                Assert.Equal(uow.Id, _uowManager.Current.Id);
            }

            // After all UoWs disposed
            Assert.Null(_uowManager.Current);
        }

        [Fact]
        public async Task UoW_MultipleOperations_ShouldAllSucceed()
        {
            // Arrange
            var orders = Enumerable.Range(1, 5).Select(i => new Order
            {
                OrderNumber = $"ORD-{100 + i}",
                CustomerName = $"Customer {i}",
                TotalAmount = i * 100m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            }).ToList();

            // Act
            using (var uow = _uowManager.Begin())
            {
                var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
                foreach (var order in orders)
                {
                    dbContext.Orders.Add(order);
                }
                await dbContext.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Assert
            var dbContext2 = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var savedOrders = await dbContext2.Orders
                .Where(o => o.OrderNumber.StartsWith("ORD-1"))
                .ToListAsync();
            Assert.Equal(5, savedOrders.Count);
        }

        [Fact]
        public async Task UoW_WithIsolationLevel_ShouldRespectIsolation()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-200",
                CustomerName = "Isolation Test",
                TotalAmount = 500.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            using (var uow = _uowManager.Begin(new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            }))
            {
                Assert.Equal(IsolationLevel.ReadCommitted, uow.IsolationLevel);

                var dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
                dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Assert
            var dbContext2 = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TestDbContext>();
            var savedOrder = await dbContext2.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-200");
            Assert.NotNull(savedOrder);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
