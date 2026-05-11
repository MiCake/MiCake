using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Util.Query.Dynamic;
using MiCake.Util.Query.Paging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Tests.Repository
{
    public class EFRepositoryHasPagingTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly Mock<IUnitOfWorkManager> _mockUnitOfWorkManager;
        private readonly Mock<IEFCoreContextFactory<TestDbContext>> _mockContextFactory;
        private readonly Mock<ILogger<TestPagingRepository>> _mockLogger;
        private readonly Mock<ILogger<EFRepositoryDependencies<TestDbContext>>> _mockDependenciesLogger;
        private readonly TestPagingRepository _repository;

        public EFRepositoryHasPagingTests()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new TestDbContext(options);
            _mockUnitOfWorkManager = new Mock<IUnitOfWorkManager>();
            _mockContextFactory = new Mock<IEFCoreContextFactory<TestDbContext>>();
            _mockLogger = new Mock<ILogger<TestPagingRepository>>();
            _mockDependenciesLogger = new Mock<ILogger<EFRepositoryDependencies<TestDbContext>>>();

            var optionsAccessor = new MiCakeEFCoreOptions(typeof(TestDbContext));

            var dependencies = new EFRepositoryDependencies<TestDbContext>(
                _mockContextFactory.Object,
                _mockUnitOfWorkManager.Object,
                _mockDependenciesLogger.Object,
                optionsAccessor);

            _repository = new TestPagingRepository(dependencies);
        }

        [Fact]
        public async Task CommonFilterPagingQueryAsync_Returns_Correct_Page_And_TotalCount()
        {
            // Arrange: create 25 items
            var items = Enumerable.Range(1, 25).Select(i => new TestEntity { Name = $"Item {i}" }).ToArray();
            _dbContext.TestEntities.AddRange(items);
            await _dbContext.SaveChangesAsync();

            // Setup UoW and factory to return the backing context
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act: request page 1 with page size 10
            var response = await _repository.FilterPagingQueryAsync(new PagingRequest(1, 10), FilterGroup.Create(new List<Filter> { Filter.Create(nameof(TestEntity.Name), new List<FilterValue> { FilterValue.Create("Item", ValueOperatorType.Contains) }) }));

            // Assert
            Assert.Equal(1, response.CurrentIndex);
            Assert.Equal(25, response.TotalCount);
            Assert.Equal(10, response.Data.Count());
        }

        [Fact]
        public async Task CommonFilterPagingQueryAsync_Applies_Filter_Correctly_And_Returns_TotalCountOfFiltered()
        {
            // Arrange: create 10 matching "match" items and 20 others
            var matching = Enumerable.Range(1, 10).Select(i => new TestEntity { Name = $"match-{i}" });
            var others = Enumerable.Range(1, 20).Select(i => new TestEntity { Name = $"other-{i}" });
            _dbContext.TestEntities.AddRange(matching.Concat(others));
            await _dbContext.SaveChangesAsync();

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            var filter = Filter.Create(nameof(TestEntity.Name), new List<FilterValue> { FilterValue.Create("match", ValueOperatorType.Contains) });
            var group = FilterGroup.Create(new List<Filter> { filter });

            // Act: page request (page size is larger than number of matches)
            var response = await _repository.FilterPagingQueryAsync(new PagingRequest(1, 50), group);

            // Assert: total count should be only matching items
            Assert.Equal(10, response.TotalCount);
            Assert.Equal(10, response.Data.Count());
            Assert.All(response.Data, e => Assert.StartsWith("match-", e.Name));
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #region Test helpers

        private class TestPagingRepository : EFRepositoryHasPaging<TestDbContext, TestEntity, int>
        {
            public TestPagingRepository(EFRepositoryDependencies<TestDbContext> dependencies) : base(dependencies) { }

            // Expose dbcontext helpers via protected methods if needed (using base methods)
        }

        #endregion
    }

    // Move test DbContext and entity to namespace-level and make them public so Moq can proxy generic types
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities { get; set; }
    }

    public class TestEntity : MiCake.DDD.Domain.AggregateRoot<int>
    {
        public string Name { get; set; }
    }
    }
