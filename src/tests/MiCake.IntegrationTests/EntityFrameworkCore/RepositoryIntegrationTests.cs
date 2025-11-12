using MiCake.Core;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.IntegrationTests.EntityFrameworkCore
{
    /// <summary>
    /// Custom repository for Order aggregate root for testing
    /// </summary>
    public class OrderRepository : EFRepository<TestDbContext, Order, int>
    {
        public OrderRepository(EFRepositoryDependencies<TestDbContext> dependencies) : base(dependencies)
        {
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(string customerName)
        {
            return await Query()
                .Where(o => o.CustomerName == customerName)
                .ToListAsync();
        }

        public async Task<List<Order>> GetPendingOrdersAsync()
        {
            return await Query()
                .Where(o => o.Status == OrderStatus.Pending)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Custom repository for Product aggregate root for testing
    /// </summary>
    public class ProductRepository : EFRepository<TestDbContext, Product, int>
    {
        public ProductRepository(EFRepositoryDependencies<TestDbContext> dependencies) : base(dependencies)
        {
        }

        public async Task<List<Product>> GetActiveProductsAsync()
        {
            return await Query()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Integration tests for Repository functionality with EF Core
    /// Tests CRUD operations, queries, and integration with UoW
    /// </summary>
    public class RepositoryIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkManager _uowManager;

        public RepositoryIntegrationTests()
        {
            var services = new ServiceCollection();

            // Configure SQLite in-memory database (better support than EF InMemory provider)
            var dbName = $"DataSource=:memory:";
            var connection = new Microsoft.Data.Sqlite.SqliteConnection(dbName);
            connection.Open(); // Keep connection open for in-memory SQLite to persist

            services.AddSingleton(connection);
            services.AddDbContext<TestDbContext>(options =>
                options.UseSqlite(connection));

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
                    options.ImplicitModeForUow = false; // Explicit UoW mode
                    options.WillOpenTransactionForUow = true; // Enable transactions
                });

            builder.Build();

            // Register custom repositories
            services.AddScoped<OrderRepository>();
            services.AddScoped<ProductRepository>();

            _serviceProvider = services.BuildServiceProvider();
            
            // Create database schema
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                dbContext.Database.EnsureCreated();
            }

            _uowManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        public async Task Repository_Add_ShouldPersistEntity()
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
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                await repository.AddAsync(order);
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Assert
            using (var scope = _serviceProvider.CreateScope())
            {
                using (var uow = _uowManager.Begin())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<OrderRepository>();
                    var savedOrder = await repository.FindAsync(order.Id);
                    Assert.NotNull(savedOrder);
                    Assert.Equal("ORD-001", savedOrder.OrderNumber);
                    Assert.Equal("John Doe", savedOrder.CustomerName);
                }
            }
        }

        [Fact]
        public async Task Repository_AddAndReturn_ShouldReturnEntityWithId()
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
            Order savedOrder;
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                savedOrder = await repository.AddAndReturnAsync(order, saveNow: true);
                await uow.CommitAsync();
            }

            // Assert
            Assert.True(savedOrder.Id > 0);
            Assert.Equal("ORD-002", savedOrder.OrderNumber);
        }

        [Fact]
        public async Task Repository_Update_ShouldModifyEntity()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-003",
                CustomerName = "Alice",
                TotalAmount = 150.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Add order first
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                await repository.AddAsync(order);
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Act - Update the order
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                var orderToUpdate = await repository.FindAsync(order.Id);
                Assert.NotNull(orderToUpdate);

                orderToUpdate.CustomerName = "Alice Updated";
                orderToUpdate.TotalAmount = 175.00m;
                await repository.UpdateAsync(orderToUpdate);
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Assert
            using (var scope = _serviceProvider.CreateScope())
            {
                using (var uow = _uowManager.Begin())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<OrderRepository>();
                    var updatedOrder = await repository.FindAsync(order.Id);
                    Assert.NotNull(updatedOrder);
                    Assert.Equal("Alice Updated", updatedOrder.CustomerName);
                    Assert.Equal(175.00m, updatedOrder.TotalAmount);
                }
            }
        }

        [Fact]
        public async Task Repository_Delete_ShouldRemoveEntity()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-004",
                CustomerName = "Bob",
                TotalAmount = 250.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Add order first
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                await repository.AddAsync(order);
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Act - Delete the order
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                var orderToDelete = await repository.FindAsync(order.Id);
                Assert.NotNull(orderToDelete);

                await repository.DeleteAsync(orderToDelete);
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Assert
            using (var scope = _serviceProvider.CreateScope())
            {
                using (var uow = _uowManager.Begin())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<OrderRepository>();
                    var deletedOrder = await repository.FindAsync(order.Id);
                    Assert.Null(deletedOrder);
                }
            }
        }

        [Fact]
        public async Task Repository_DeleteById_ShouldRemoveEntity()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-005",
                CustomerName = "Charlie",
                TotalAmount = 300.00m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Add order first
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                await repository.AddAsync(order);
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            var orderId = order.Id;

            // Act - Delete by ID without loading
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                await repository.DeleteByIdAsync(orderId);
                await uow.CommitAsync();
            }

            // Assert
            using (var scope = _serviceProvider.CreateScope())
            {
                using (var uow = _uowManager.Begin())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<OrderRepository>();
                    var deletedOrder = await repository.FindAsync(orderId);
                    Assert.Null(deletedOrder);
                }
            }
        }

        [Fact]
        public async Task Repository_Query_ShouldReturnFilteredResults()
        {
            // Arrange - Add multiple orders
            var orders = new[]
            {
                new Order { OrderNumber = "ORD-101", CustomerName = "Customer A", TotalAmount = 100m, Status = OrderStatus.Pending, CreatedDate = DateTime.UtcNow },
                new Order { OrderNumber = "ORD-102", CustomerName = "Customer A", TotalAmount = 200m, Status = OrderStatus.Pending, CreatedDate = DateTime.UtcNow },
                new Order { OrderNumber = "ORD-103", CustomerName = "Customer B", TotalAmount = 300m, Status = OrderStatus.Completed, CreatedDate = DateTime.UtcNow },
                new Order { OrderNumber = "ORD-104", CustomerName = "Customer A", TotalAmount = 400m, Status = OrderStatus.Shipped, CreatedDate = DateTime.UtcNow }
            };

            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                foreach (var order in orders)
                {
                    await repository.AddAsync(order);
                }
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Act - Query for Customer A orders
            List<Order> customerAOrders;
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                customerAOrders = await repository.GetOrdersByCustomerAsync("Customer A");
            }

            // Assert
            Assert.Equal(3, customerAOrders.Count);
            Assert.All(customerAOrders, o => Assert.Equal("Customer A", o.CustomerName));
        }

        [Fact]
        public async Task Repository_GetCount_ShouldReturnCorrectCount()
        {
            // Arrange - Add multiple products
            var products = Enumerable.Range(1, 10).Select(i => new Product
            {
                Name = $"Product {i}",
                Description = $"Description {i}",
                Price = i * 10m,
                StockQuantity = i * 5,
                IsActive = i % 2 == 0 // Every other product is active
            }).ToList();

            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<ProductRepository>();
                foreach (var product in products)
                {
                    await repository.AddAsync(product);
                }
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Act
            long totalCount;
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<ProductRepository>();
                totalCount = await repository.GetCountAsync();
            }

            // Assert
            Assert.Equal(10, totalCount);
        }

        [Fact]
        public async Task Repository_CustomQuery_ShouldWork()
        {
            // Arrange
            var products = new[]
            {
                new Product { Name = "Active Product 1", Price = 100m, IsActive = true },
                new Product { Name = "Inactive Product", Price = 200m, IsActive = false },
                new Product { Name = "Active Product 2", Price = 300m, IsActive = true }
            };

            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<ProductRepository>();
                foreach (var product in products)
                {
                    await repository.AddAsync(product);
                }
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Act - Use custom repository method
            List<Product> activeProducts;
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<ProductRepository>();
                activeProducts = await repository.GetActiveProductsAsync();
            }

            // Assert
            Assert.Equal(2, activeProducts.Count);
            Assert.All(activeProducts, p => Assert.True(p.IsActive));
            Assert.Equal("Active Product 1", activeProducts[0].Name); // Ordered by name
        }

        [Fact]
        public async Task Repository_ClearChangeTracking_ShouldDetachEntities()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-201",
                CustomerName = "Test User",
                TotalAmount = 500m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                await repository.AddAsync(order);
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Act
            using (var uow = _uowManager.Begin())
            {
                var repository = _serviceProvider.GetRequiredService<OrderRepository>();
                var loadedOrder = await repository.FindAsync(order.Id);
                Assert.NotNull(loadedOrder);

                // Modify the entity
                loadedOrder.CustomerName = "Modified Name";

                // Clear change tracking
                await repository.ClearChangeTrackingAsync();

                // Save should not persist the modification
                await repository.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Assert - Name should not be modified
            using (var scope = _serviceProvider.CreateScope())
            {
                using (var uow = _uowManager.Begin())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<OrderRepository>();
                    var reloadedOrder = await repository.FindAsync(order.Id);
                    Assert.NotNull(reloadedOrder);
                    Assert.Equal("Test User", reloadedOrder.CustomerName); // Not modified
                }
            }
        }

        [Fact]
        public async Task Repository_MultipleRepositories_ShouldWorkTogether()
        {
            // Arrange
            var order = new Order
            {
                OrderNumber = "ORD-301",
                CustomerName = "Multi Repo Test",
                TotalAmount = 1000m,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            var product = new Product
            {
                Name = "Test Product",
                Description = "For multi-repo test",
                Price = 100m,
                StockQuantity = 50,
                IsActive = true
            };

            // Act - Use both repositories in same UoW
            using (var uow = _uowManager.Begin())
            {
                var orderRepo = _serviceProvider.GetRequiredService<OrderRepository>();
                var productRepo = _serviceProvider.GetRequiredService<ProductRepository>();

                await orderRepo.AddAsync(order);
                await productRepo.AddAsync(product);

                await orderRepo.SaveChangesAsync();
                await uow.CommitAsync();
            }

            // Assert
            using (var scope = _serviceProvider.CreateScope())
            {
                using (var uow = _uowManager.Begin())
                {
                    var orderRepo = scope.ServiceProvider.GetRequiredService<OrderRepository>();
                    var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();

                    var savedOrder = await orderRepo.FindAsync(order.Id);
                    var savedProduct = await productRepo.FindAsync(product.Id);

                    Assert.NotNull(savedOrder);
                    Assert.NotNull(savedProduct);
                    Assert.Equal("ORD-301", savedOrder.OrderNumber);
                    Assert.Equal("Test Product", savedProduct.Name);
                }
            }
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
