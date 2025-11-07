using MiCake.IntegrationTests.Fakes;
using MiCake.IntegrationTests.Infrastructure;
using MiCake.DDD.Uow;
using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;

namespace MiCake.IntegrationTests
{
    /// <summary>
    /// Integration tests for Repository pattern with EF Core
    /// </summary>
    public class Repository_IntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task Repository_Add_ShouldPersistEntity()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();

            // Act
            using (var uow = uowManager.Begin())
            {
                var product = new Product
                {
                    Name = "New Product",
                    Price = 49.99m,
                    Stock = 100
                };
                await repository.AddAsync(product);
                await uow.CommitAsync();

                // Assert
                Assert.True(product.Id > 0);
            }

            // Verify persistence
            var dbContext = GetDbContext();
            var saved = await dbContext.Products.FindAsync(1);
            Assert.NotNull(saved);
            Assert.Equal("New Product", saved.Name);
        }

        [Fact]
        public async Task Repository_Update_ShouldModifyEntity()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            int productId;

            // Create product first
            using (var uow1 = uowManager.Begin())
            {
                var product = new Product { Name = "Original", Price = 10m, Stock = 5 };
                await repository.AddAsync(product);
                await uow1.CommitAsync();
                productId = product.Id;
            }

            // Act - Update product
            using (var uow2 = uowManager.Begin())
            {
                var product = await repository.FindAsync(productId);
                product.Name = "Updated";
                product.Price = 20m;
                await repository.UpdateAsync(product);
                await uow2.CommitAsync();
            }

            // Assert
            var dbContext = GetDbContext();
            var updated = await dbContext.Products.FindAsync(productId);
            Assert.Equal("Updated", updated?.Name);
            Assert.Equal(20m, updated?.Price);
        }

        [Fact]
        public async Task Repository_Delete_ShouldRemoveEntity()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            int productId;

            using (var uow = uowManager.Begin())
            {
                var product = new Product { Name = "To Delete", Price = 99m, Stock = 1 };
                await repository.AddAsync(product);
                await uow.CommitAsync();
                productId = product.Id;
            }

            // Act
            using (var uow = uowManager.Begin())
            {
                var product = await repository.FindAsync(productId);
                await repository.DeleteAsync(product);
                await uow.CommitAsync();
            }

            // Assert
            var dbContext = GetDbContext();
            var deleted = await dbContext.Products.FindAsync(productId);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Repository_Query_ShouldFilterEntities()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            using (var uow = uowManager.Begin())
            {
                await repository.AddAsync(new Product { Name = "Cheap Product", Price = 10m, Stock = 50 });
                await repository.AddAsync(new Product { Name = "Expensive Product", Price = 100m, Stock = 5 });
                await repository.AddAsync(new Product { Name = "Mid Product", Price = 50m, Stock = 20 });
                await uow.CommitAsync();
            }

            // Act
            // SQLite doesn't support ordering by decimal in queries, so we use AsEnumerable to sort client-side
            var expensiveProducts = (await dbContext.Products
                .Where(p => p.Price > 40m)
                .ToListAsync())
                .OrderBy(p => p.Price)
                .ToList();

            // Assert
            Assert.Equal(2, expensiveProducts.Count);
            Assert.Equal("Mid Product", expensiveProducts[0].Name);
            Assert.Equal("Expensive Product", expensiveProducts[1].Name);
        }

        [Fact]
        public async Task Repository_FindAsync_ShouldRetrieveEntityById()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            int productId;

            using (var uow = uowManager.Begin())
            {
                var product = new Product { Name = "Findable", Price = 75m, Stock = 15 };
                await repository.AddAsync(product);
                await uow.CommitAsync();
                productId = product.Id;
            }

            // Act
            var found = await repository.FindAsync(productId);

            // Assert
            Assert.NotNull(found);
            Assert.Equal("Findable", found.Name);
            Assert.Equal(75m, found.Price);
        }

        [Fact]
        public async Task Repository_GetAsync_NonExistentId_ShouldReturnNull()
        {
            // Arrange
            var repository = GetService<IRepository<Product, int>>();

            // Act
            var notFound = await repository.FindAsync(99999);

            // Assert
            Assert.Null(notFound);
        }

        [Fact]
        public async Task Repository_MultipleOperations_ShouldWorkInSingleUOW()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var productRepo = GetService<IRepository<Product, int>>();
            var orderRepo = GetService<IRepository<Order, int>>();

            // Act
            using (var uow = uowManager.Begin())
            {
                var product = new Product { Name = "Batch Product", Price = 30m, Stock = 100 };
                await productRepo.AddAsync(product);

                var order = new Order
                {
                    OrderNumber = "ORD-BATCH",
                    ProductId = product.Id,
                    Quantity = 5,
                    TotalAmount = 150m,
                    Status = OrderStatus.Pending
                };
                await orderRepo.AddAsync(order);

                await uow.CommitAsync();
            }

            // Assert
            var dbContext = GetDbContext();
            var products = await dbContext.Products.CountAsync();
            var orders = await dbContext.Orders.CountAsync();
            Assert.Equal(1, products);
            Assert.Equal(1, orders);
        }
    }
}
