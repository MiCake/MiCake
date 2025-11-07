using MiCake.IntegrationTests.Fakes;
using MiCake.IntegrationTests.Infrastructure;
using MiCake.DDD.Uow;
using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MiCake.IntegrationTests
{
    /// <summary>
    /// Advanced integration tests for Repository pattern with complex scenarios
    /// </summary>
    public class Repository_Advanced_IntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task Repository_DbContext_ShouldSupportLinqOperations()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            // Create test data
            using (var uow = uowManager.Begin())
            {
                for (int i = 1; i <= 5; i++)
                {
                    var product = new Product
                    {
                        Name = $"Product {i}",
                        Price = 10m * i,
                        Stock = i * 10
                    };
                    await repository.AddAsync(product);
                }
                await uow.CommitAsync();
            }

            // Act - Query with Where clause
            var expensiveProducts = dbContext.Products.Where(p => p.Price > 20m).ToList();

            // Assert
            Assert.NotEmpty(expensiveProducts);
            Assert.All(expensiveProducts, p => Assert.True(p.Price > 20m));
        }

        [Fact]
        public async Task Repository_DbContext_ShouldSupportOrdering()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            // Create test data
            using (var uow = uowManager.Begin())
            {
                foreach (var p in new List<Product>
                {
                    new Product { Name = "Expensive", Price = 100m, Stock = 10 },
                    new Product { Name = "Cheap", Price = 10m, Stock = 50 },
                    new Product { Name = "Medium", Price = 50m, Stock = 30 }
                })
                {
                    await repository.AddAsync(p);
                }
                await uow.CommitAsync();
            }

            // Act - Order by price ascending (using ToList() first for SQLite compatibility)
            var orderedAsc = dbContext.Products
                .ToList()  // Bring to client to avoid SQLite decimal ORDER BY limitation
                .OrderBy(p => p.Price)
                .ToList();

            // Assert
            Assert.Equal(3, orderedAsc.Count);
            Assert.True(orderedAsc[0].Price <= orderedAsc[1].Price);
            Assert.True(orderedAsc[1].Price <= orderedAsc[2].Price);
        }

        [Fact]
        public async Task Repository_DbContext_ShouldSupportAggregations()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            // Create test data
            using (var uow = uowManager.Begin())
            {
                for (int i = 1; i <= 3; i++)
                {
                    await repository.AddAsync(new Product
                    {
                        Name = $"Product {i}",
                        Price = 50m,
                        Stock = 20
                    });
                }
                await uow.CommitAsync();
            }

            // Act - Count (works fine)
            var count = dbContext.Products.Count();

            // For aggregations with decimal, bring to client to avoid SQLite limitations
            var products = dbContext.Products.ToList();
            var maxPrice = products.Max(p => p.Price);
            var avgPrice = products.Average(p => p.Price);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(50m, maxPrice);
            Assert.Equal(50m, avgPrice);
        }

        [Fact]
        public async Task Repository_DbContext_ShouldSupportPagination()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            // Create test data - 10 products
            using (var uow = uowManager.Begin())
            {
                for (int i = 1; i <= 10; i++)
                {
                    await repository.AddAsync(new Product
                    {
                        Name = $"Product {i}",
                        Price = 10m * i,
                        Stock = i
                    });
                }
                await uow.CommitAsync();
            }

            // Act - Pagination
            int pageSize = 3;
            var page1 = dbContext.Products
                .OrderBy(p => p.Id)
                .Skip(0)
                .Take(pageSize)
                .ToList();

            var page2 = dbContext.Products
                .OrderBy(p => p.Id)
                .Skip(pageSize)
                .Take(pageSize)
                .ToList();

            // Assert
            Assert.Equal(3, page1.Count);
            Assert.Equal(3, page2.Count);
            Assert.NotEmpty(page1);
            Assert.NotEmpty(page2);
            Assert.DoesNotContain(page1[0], page2); // No overlap
        }

        [Fact]
        public async Task Repository_DbContext_ShouldSupportFiltering()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            // Create test data
            using (var uow = uowManager.Begin())
            {
                var products = new[]
                {
                    new Product { Name = "In Stock", Price = 50m, Stock = 10 },
                    new Product { Name = "Out of Stock", Price = 100m, Stock = 0 },
                    new Product { Name = "Low Stock", Price = 30m, Stock = 2 }
                };
                foreach (var p in products)
                {
                    await repository.AddAsync(p);
                }
                await uow.CommitAsync();
            }

            // Act - Filter by multiple conditions
            var available = dbContext.Products
                .Where(p => p.Stock > 0 && p.Price < 80m)
                .ToList();

            var outOfStock = dbContext.Products
                .Where(p => p.Stock == 0)
                .ToList();

            // Assert
            Assert.NotEmpty(available);
            Assert.All(available, p => Assert.True(p.Stock > 0 && p.Price < 80m));
            Assert.Single(outOfStock);
            Assert.Equal(0, outOfStock[0].Stock);
        }

        [Fact]
        public async Task Repository_DbContext_ShouldSupportProjection()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            // Create test data
            using (var uow = uowManager.Begin())
            {
                await repository.AddAsync(new Product
                {
                    Name = "Test Product",
                    Price = 99.99m,
                    Stock = 50
                });
                await uow.CommitAsync();
            }

            // Act - Project to anonymous type
            var projection = dbContext.Products
                .Select(p => new { p.Name, p.Price })
                .FirstOrDefault();

            // Assert
            Assert.NotNull(projection);
            Assert.Equal("Test Product", projection.Name);
            Assert.Equal(99.99m, projection.Price);
        }

        [Fact]
        public async Task Repository_DbContext_ShouldSupportComplexConditions()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            // Create test data
            using (var uow = uowManager.Begin())
            {
                var products = new[]
                {
                    new Product { Name = "Premium", Price = 150m, Stock = 5 },
                    new Product { Name = "Standard", Price = 50m, Stock = 20 },
                    new Product { Name = "Budget", Price = 20m, Stock = 100 }
                };
                foreach (var p in products)
                {
                    await repository.AddAsync(p);
                }
                await uow.CommitAsync();
            }

            // Act - Complex query with AND, OR
            var results = dbContext.Products
                .Where(p => (p.Price > 100m && p.Stock < 10m) || (p.Price < 30m && p.Stock > 50m))
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, p =>
            {
                bool condition1 = p.Price > 100m && p.Stock < 10m;
                bool condition2 = p.Price < 30m && p.Stock > 50m;
                Assert.True(condition1 || condition2);
            });
        }

        [Fact]
        public async Task Repository_MultipleOperations_ShouldMaintainConsistency()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            int productId;

            // Create product
            using (var uow = uowManager.Begin())
            {
                var product = new Product { Name = "Original", Price = 50m, Stock = 100 };
                await repository.AddAsync(product);
                await uow.CommitAsync();
                productId = product.Id;
            }

            // Act - Multiple updates
            using (var uow = uowManager.Begin())
            {
                var product = await repository.FindAsync(productId);
                product.Price = 75m;
                await repository.UpdateAsync(product);
                await uow.CommitAsync();
            }

            // Act - Verify changes persisted
            using (var uow = uowManager.Begin())
            {
                var product = await repository.FindAsync(productId);
                product.Stock = 50;
                await repository.UpdateAsync(product);
                await uow.CommitAsync();
            }

            // Assert - Final state
            using (var uow = uowManager.Begin())
            {
                var finalProduct = await repository.FindAsync(productId);
                Assert.NotNull(finalProduct);
                Assert.Equal(75m, finalProduct.Price);
                Assert.Equal(50, finalProduct.Stock);
            }
        }

        [Fact]
        public async Task Repository_DbContext_ShouldSupportGrouping()
        {
            // Arrange
            var uowManager = GetService<IUnitOfWorkManager>();
            var repository = GetService<IRepository<Product, int>>();
            var dbContext = GetDbContext();

            // Create test data
            using (var uow = uowManager.Begin())
            {
                var products = new[]
                {
                    new Product { Name = "Expensive 1", Price = 100m, Stock = 5 },
                    new Product { Name = "Expensive 2", Price = 100m, Stock = 10 },
                    new Product { Name = "Cheap 1", Price = 20m, Stock = 50 }
                };
                foreach (var p in products)
                {
                    await repository.AddAsync(p);
                }
                await uow.CommitAsync();
            }

            // Act - Group by price (using ToList() first for SQLite compatibility with decimal)
            var grouped = dbContext.Products
                .ToList()  // Bring to client to avoid SQLite decimal grouping/aggregation issues
                .GroupBy(p => p.Price)
                .Select(g => new { Price = g.Key, Count = g.Count(), AvgStock = g.Average(p => p.Stock) })
                .ToList();

            // Assert
            Assert.NotEmpty(grouped);
            Assert.Contains(grouped, g => g.Price == 100m && g.Count == 2);
            Assert.Contains(grouped, g => g.Price == 20m && g.Count == 1);
        }
    }
}
