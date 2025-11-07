using MiCake.Audit.Core;
using MiCake.IntegrationTests.Fakes;
using MiCake.IntegrationTests.Infrastructure;
using MiCake.DDD.Uow;
using MiCake.DDD.Domain;

namespace MiCake.IntegrationTests
{
    /// <summary>
    /// Integration tests for Audit functionality
    /// </summary>
    public class Audit_IntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task Audit_CreationTime_ShouldBeSetAutomatically()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var beforeCreate = DateTime.Now;

            // Act
            Product createdProduct;
            using (var uow = uowManager.Begin())
            {
                var product = new Product { Name = "Audit Product", Price = 100m, Stock = 10 };
                await repository.AddAsync(product);
                await uow.CommitAsync();
                createdProduct = product;
            }

            var afterCreate = DateTime.Now;

            // Assert
            Assert.NotEqual(default(DateTime), createdProduct.CreationTime);
            Assert.True(createdProduct.CreationTime >= beforeCreate && createdProduct.CreationTime <= afterCreate);
        }

        [Fact]
        public async Task Audit_ModificationTime_ShouldUpdateOnChange()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();

            // Act - Create
            Product product;
            using (var uow = uowManager.Begin())
            {
                product = new Product { Name = "Modification Test", Price = 100m, Stock = 10 };
                await repository.AddAsync(product);
                await uow.CommitAsync();
            }

            var creationTime = product.CreationTime;
            await Task.Delay(100); // Ensure time difference

            // Act - Update
            using (var uow = uowManager.Begin())
            {
                product.UpdatePrice(150m);
                await repository.UpdateAsync(product);
                await uow.CommitAsync();
            }

            // Assert
            Assert.NotNull(product.ModificationTime);
            Assert.True(product.ModificationTime > creationTime);
        }

        [Fact]
        public async Task Audit_SoftDeletion_ShouldSetDeletionTimeAndFlag()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Order, int>>();

            // Act - Create
            Order order;
            using (var uow = uowManager.Begin())
            {
                order = new Order
                {
                    OrderNumber = "SOFT-DELETE",
                    ProductId = 1,
                    Quantity = 2,
                    TotalAmount = 200m,
                    Status = OrderStatus.Pending
                };
                await repository.AddAsync(order);
                await uow.CommitAsync();
            }

            // Act - Soft Delete (must use DeleteAsync to trigger soft deletion)
            var beforeDelete = DateTime.Now;
            using (var uow = uowManager.Begin())
            {
                await repository.DeleteAsync(order);
                await uow.CommitAsync();
            }
            var afterDelete = DateTime.Now;

            // Assert
            Assert.True(order.IsDeleted);
            Assert.NotNull(order.DeletionTime);
            Assert.True(order.DeletionTime >= beforeDelete && order.DeletionTime <= afterDelete);
        }

                [Fact]
        public async Task Audit_MultipleEntities_ShouldHaveIndependentAuditInfo()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();

            // Act - Create two products in separate UOWs to ensure different timestamps
            int product1Id, product2Id;
            using (var uow = uowManager.Begin())
            {
                var product1 = new Product { Name = "Product 1", Price = 100m, Stock = 10 };
                await repository.AddAsync(product1);
                await uow.CommitAsync();
                product1Id = product1.Id;
            }

            await Task.Delay(100); // Delay for time difference
            
            using (var uow = uowManager.Begin())
            {
                var product2 = new Product { Name = "Product 2", Price = 200m, Stock = 20 };
                await repository.AddAsync(product2);
                await uow.CommitAsync();
                product2Id = product2.Id;
            }

            // Assert - Query from DbContext directly since FindAsync might cache
            using (var uow = uowManager.Begin())
            {
                var dbContext = GetDbContext();
                var savedProduct1 = await dbContext.Products.FindAsync(product1Id);
                var savedProduct2 = await dbContext.Products.FindAsync(product2Id);
                
                Assert.NotNull(savedProduct1);
                Assert.NotNull(savedProduct2);
                Assert.NotEqual(default(DateTime), savedProduct1.CreationTime);
                Assert.NotEqual(default(DateTime), savedProduct2.CreationTime);
                Assert.True(savedProduct2.CreationTime >= savedProduct1.CreationTime);
            }
        }

        [Fact]
        public async Task Audit_CustomTimeProvider_ShouldBeRespected()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var customTime = new DateTime(2024, 1, 1, 12, 0, 0);
            
            DefaultTimeAuditProvider.CurrentTimeProvider = () => customTime;

            try
            {
                // Act
                Product product;
                using (var uow = uowManager.Begin())
                {
                    product = new Product { Name = "Custom Time Product", Price = 100m, Stock = 10 };
                    await repository.AddAsync(product);
                    await uow.CommitAsync();
                }

                // Assert
                Assert.Equal(customTime, product.CreationTime);
            }
            finally
            {
                // Cleanup
                DefaultTimeAuditProvider.CurrentTimeProvider = null;
            }
        }

        [Fact]
        public async Task Audit_BothCreateAndModify_ShouldTrackBothTimes()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();

            // Act - Create
            Product product;
            using (var uow = uowManager.Begin())
            {
                product = new Product { Name = "Full Audit", Price = 100m, Stock = 10 };
                await repository.AddAsync(product);
                await uow.CommitAsync();
            }

            var creationTime = product.CreationTime;
            await Task.Delay(100);

            // Act - Modify
            using (var uow = uowManager.Begin())
            {
                product.UpdatePrice(150m);
                await repository.UpdateAsync(product);
                await uow.CommitAsync();
            }

            // Assert
            Assert.NotEqual(default(DateTime), product.CreationTime);
            Assert.NotNull(product.ModificationTime);
            Assert.Equal(creationTime, product.CreationTime); // Creation time unchanged
            Assert.True(product.ModificationTime > product.CreationTime);
        }
    }
}
