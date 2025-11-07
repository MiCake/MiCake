using MiCake.IntegrationTests.Fakes;
using MiCake.IntegrationTests.Infrastructure;
using MiCake.DDD.Uow;
using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;

namespace MiCake.IntegrationTests
{
    /// <summary>
    /// Integration tests for domain event functionality.
    /// Tests must run sequentially because event handlers use static collections.
    /// </summary>
    [Collection("Sequential")]
    public class DomainEvent_IntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task DomainEvent_ShouldDispatch_AfterSaveChanges()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            ProductPriceChangedHandler.Clear();

            // Act - Add product first
            int productId;
            using (var uow1 = uowManager.Begin())
            {
                var product = new Product { Name = "Event Product", Price = 100m, Stock = 10 };
                await repository.AddAsync(product);
                await uow1.CommitAsync();
                productId = product.Id;
            }

            // Act - Update price (no need to call UpdateAsync, EF Core tracks changes)
            using (var uow2 = uowManager.Begin())
            {
                var product = await repository.FindAsync(productId);
                product.UpdatePrice(150m);
                // await repository.UpdateAsync(product); // Not needed - EF tracks changes
                await uow2.CommitAsync();
            }

            // Assert
            Assert.Single(ProductPriceChangedHandler.HandledEvents);
            var evt = ProductPriceChangedHandler.HandledEvents[0];
            Assert.Equal(100m, evt.OldPrice);
            Assert.Equal(150m, evt.NewPrice);
        }

        [Fact]
        public async Task DomainEvent_MultipleEvents_ShouldDispatchAll()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            ProductPriceChangedHandler.Clear();
            ProductStockDecreasedHandler.Clear();

            // Act - Add product first
            int productId;
            using (var uow1 = uowManager.Begin())
            {
                var product = new Product { Name = "Multi Event Product", Price = 200m, Stock = 50 };
                await repository.AddAsync(product);
                await uow1.CommitAsync();
                productId = product.Id;
            }

            // Act - Update price and stock (no UpdateAsync needed)
            using (var uow2 = uowManager.Begin())
            {
                var product = await repository.FindAsync(productId);
                product.UpdatePrice(250m);
                product.DecreaseStock(10);
                // await repository.UpdateAsync(product); // Not needed
                await uow2.CommitAsync();
            }

            // Assert
            Assert.Single(ProductPriceChangedHandler.HandledEvents);
            Assert.Single(ProductStockDecreasedHandler.HandledEvents);
            Assert.Equal(250m, ProductPriceChangedHandler.HandledEvents[0].NewPrice);
            Assert.Equal(10, ProductStockDecreasedHandler.HandledEvents[0].Quantity);
        }

        [Fact]
        public async Task DomainEvent_NotDispatched_WithoutCommit()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Order, int>>();
            OrderCompletedHandler.Clear();

            // Act
            using (var uow = uowManager.Begin())
            {
                var order = new Order
                {
                    OrderNumber = "NO-COMMIT",
                    ProductId = 1,
                    Quantity = 1,
                    TotalAmount = 100m,
                    Status = OrderStatus.Pending
                };
                await repository.AddAsync(order);
                await uow.CommitAsync();
                
                order.Complete();
                // Don't commit
            }

            // Assert - Event should not be dispatched
            Assert.Empty(OrderCompletedHandler.HandledEvents);
        }

        [Fact]
        public async Task DomainEvent_OrderLifecycle_ShouldDispatchCorrectEvents()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var orderRepo = GetService<IRepository<Order, int>>();
            OrderCompletedHandler.Clear();

            // Act - Create order first
            int orderId;
            using (var uow1 = uowManager.Begin())
            {
                var order = new Order
                {
                    OrderNumber = "LIFECYCLE-001",
                    ProductId = 1,
                    Quantity = 3,
                    TotalAmount = 300m,
                    Status = OrderStatus.Pending
                };
                await orderRepo.AddAsync(order);
                await uow1.CommitAsync();
                orderId = order.Id;
            }

            // Act - Complete order (no UpdateAsync needed)
            using (var uow2 = uowManager.Begin())
            {
                var order = await orderRepo.FindAsync(orderId);
                order.Complete();
                // await orderRepo.UpdateAsync(order); // Not needed
                await uow2.CommitAsync();
            }

            // Assert
            Assert.Single(OrderCompletedHandler.HandledEvents);
            Assert.Equal("LIFECYCLE-001", OrderCompletedHandler.HandledEvents[0].OrderNumber);
        }

        [Fact]
        public async Task DomainEvent_MultipleAggregates_ShouldDispatchAllEvents()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var productRepo = GetService<IRepository<Product, int>>();
            var orderRepo = GetService<IRepository<Order, int>>();
            ProductPriceChangedHandler.Clear();
            OrderCompletedHandler.Clear();

            int productId;

            // Act - Create product and update price
            using (var uow1 = uowManager.Begin())
            {
                var product = new Product { Name = "Product A", Price = 80m, Stock = 20 };
                await productRepo.AddAsync(product);
                await uow1.CommitAsync();
                productId = product.Id;
            }

            using (var uow2 = uowManager.Begin())
            {
                var product = await productRepo.FindAsync(productId);
                product.UpdatePrice(90m);
                await uow2.CommitAsync();
            }

            // Act - Create and complete order
            int orderId;
            using (var uow3 = uowManager.Begin())
            {
                var order = new Order
                {
                    OrderNumber = "MULTI-AGG",
                    ProductId = productId,
                    Quantity = 2,
                    TotalAmount = 180m,
                    Status = OrderStatus.Pending
                };
                await orderRepo.AddAsync(order);
                await uow3.CommitAsync();
                orderId = order.Id;
            }

            using (var uow4 = uowManager.Begin())
            {
                var order = await orderRepo.FindAsync(orderId);
                order.Complete();
                // await orderRepo.UpdateAsync(order); // Not needed
                await uow4.CommitAsync();
            }

            // Assert - Both events should be dispatched
            Assert.Single(ProductPriceChangedHandler.HandledEvents);
            Assert.Single(OrderCompletedHandler.HandledEvents);
        }

        [Fact]
        public async Task DomainEvent_RollbackUOW_ShouldNotDispatchEvents()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            ProductPriceChangedHandler.Clear();
            int productId;

            // Create product first
            using (var uow1 = uowManager.Begin())
            {
                var product = new Product { Name = "Rollback Product", Price = 100m, Stock = 10 };
                await repository.AddAsync(product);
                await uow1.CommitAsync();
                productId = product.Id;
            }

            // Act - Update and rollback
            using (var uow2 = uowManager.Begin())
            {
                var dbContext = GetDbContext();
                var product = await dbContext.Products.FirstAsync(p => p.Id == productId);
                product.UpdatePrice(150m);
                await uow2.RollbackAsync();
            }

            // Assert - Event should not be dispatched after rollback
            Assert.Empty(ProductPriceChangedHandler.HandledEvents);
        }
    }
}
