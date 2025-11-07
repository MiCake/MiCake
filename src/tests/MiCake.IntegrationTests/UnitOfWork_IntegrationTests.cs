using MiCake.IntegrationTests.Fakes;
using MiCake.IntegrationTests.Infrastructure;
using MiCake.DDD.Uow;
using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;

namespace MiCake.IntegrationTests
{
    /// <summary>
    /// Integration tests for Unit of Work pattern
    /// </summary>
    public class UnitOfWork_IntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task UOW_ExplicitMode_ShouldNotSaveWithoutCommit()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();

            // Act
            using (var uow = uowManager.Begin())
            {
                var product = new Product
                {
                    Name = "Test Product",
                    Price = 99.99m,
                    Stock = 10
                };
                await repository.AddAsync(product);
                // Don't commit - just let UOW dispose
            }

            // Assert - Product should not be saved
            var dbContext = GetDbContext();
            var savedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Test Product");
            Assert.Null(savedProduct);
        }

        [Fact]
        public async Task UOW_ExplicitMode_ShouldSaveAfterCommit()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();

            // Act
            using (var uow = uowManager.Begin())
            {
                var product = new Product
                {
                    Name = "Committed Product",
                    Price = 149.99m,
                    Stock = 20
                };
                await repository.AddAsync(product);
                await uow.CommitAsync();
            }

            // Assert - Product should be saved
            var dbContext = GetDbContext();
            var savedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Committed Product");
            Assert.NotNull(savedProduct);
            Assert.Equal(149.99m, savedProduct.Price);
            Assert.Equal(20, savedProduct.Stock);
        }

        [Fact]
        public async Task UOW_Rollback_ShouldNotSaveChanges()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();

            // Act
            using (var uow = uowManager.Begin())
            {
                var product = new Product
                {
                    Name = "Rollback Product",
                    Price = 199.99m,
                    Stock = 5
                };
                await repository.AddAsync(product);
                await uow.RollbackAsync();
            }

            // Assert - Product should not be saved
            var dbContext = GetDbContext();
            var savedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Rollback Product");
            Assert.Null(savedProduct);
        }

        [Fact]
        public async Task UOW_NestedTransactions_ShouldHandleCorrectly()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var productRepo = GetService<IRepository<Product, int>>();
            var orderRepo = GetService<IRepository<Order, int>>();
            int productId;

            // Act - Create product first
            using (var outerUow = uowManager.Begin())
            {
                var product = new Product
                {
                    Name = "Nested Product",
                    Price = 299.99m,
                    Stock = 15
                };
                await productRepo.AddAsync(product);
                await outerUow.CommitAsync();
                productId = product.Id;
            }

            // Act - Create order with nested UOW
            using (var innerUow = uowManager.Begin())
            {
                var order = new Order
                {
                    OrderNumber = "ORD-001",
                    ProductId = productId,
                    Quantity = 2,
                    TotalAmount = 599.98m,
                    Status = OrderStatus.Pending
                };
                await orderRepo.AddAsync(order);
                await innerUow.CommitAsync();
            }

            // Assert - Both should be saved
            var dbContext = GetDbContext();
            var savedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Nested Product");
            var savedOrder = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == "ORD-001");

            Assert.NotNull(savedProduct);
            Assert.NotNull(savedOrder);
            Assert.Equal(savedProduct.Id, savedOrder.ProductId);
        }

        [Fact]
        public async Task UOW_Exception_ShouldRollbackAutomatically()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                using (var uow = uowManager.Begin())
                {
                    var product = new Product
                    {
                        Name = "Exception Product",
                        Price = 99.99m,
                        Stock = 10
                    };
                    await repository.AddAsync(product);

                    throw new InvalidOperationException("Test exception");
                }
            });

            // Assert - Product should not be saved due to exception
            var dbContext = GetDbContext();
            var savedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Exception Product");
            Assert.Null(savedProduct);
        }
    }
}
