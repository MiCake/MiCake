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
        public async Task PagingQueryAsync_WithoutSort_OrdersByPrimaryKeyAscending()
        {
            // Arrange: 25 items so a total order matters across pages.
            var items = Enumerable.Range(1, 25).Select(i => new TestEntity { Name = $"Item {i}" }).ToArray();
            _dbContext.TestEntities.AddRange(items);
            await _dbContext.SaveChangesAsync();

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var page1 = await _repository.PagingQueryAsync(new PagingRequest(1, 10));
            var page3 = await _repository.PagingQueryAsync(new PagingRequest(3, 10));

            // Assert: pages are fully ordered by the primary key ascending.
            Assert.Equal(25, page1.TotalCount);
            Assert.Equal(1, page1.Data.First().Id);
            Assert.Equal(10, page1.Data.Last().Id);
            Assert.Equal(21, page3.Data.First().Id);
            Assert.Equal(25, page3.Data.Last().Id);
        }

        [Fact]
        public async Task PagingQueryAsync_WithCallerSort_AppendsMissingPrimaryKeyForTies()
        {
            // Arrange: two names with three items each so caller sorting alone is unstable.
            var items = new List<TestEntity>
            {
                new() { Name = "same", },
                new() { Name = "same", },
                new() { Name = "same", },
                new() { Name = "other", },
                new() { Name = "other", },
            };
            _dbContext.TestEntities.AddRange(items);
            await _dbContext.SaveChangesAsync();

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act: sort by Name ascending; the primary key must be appended as the tie-breaker.
            var response = await _repository.PagingQueryAsync(new PagingRequest(1, 10), e => e.Name, asc: true);

            // Assert: caller direction preserved, ties broken by ascending primary key.
            var result = response.Data.ToList();
            Assert.Equal(5, result.Count);
            Assert.Equal("other", result[0].Name);
            Assert.Equal("other", result[1].Name);
            Assert.Equal("same", result[2].Name);
            Assert.True(result[0].Id < result[1].Id);
            Assert.True(result[2].Id < result[3].Id);
            Assert.True(result[3].Id < result[4].Id);
        }

        [Fact]
        public async Task PagingQueryAsync_KeylessEntity_ThrowsWithDiagnostic()
        {
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            var repository = new TestKeylessPagingRepository(_mockContextFactory.Object, _mockUnitOfWorkManager.Object, _mockDependenciesLogger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.PagingQueryAsync(new PagingRequest(1, 10)));

            Assert.Contains("keyless", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PagingQueryAsync_ShadowPrimaryKey_OrdersByShadowKeyAscending()
        {
            var a = new ShadowKeyEntity { Name = "a" };
            var b = new ShadowKeyEntity { Name = "b" };
            var c = new ShadowKeyEntity { Name = "c" };
            TrackWithShadowKey(a, "ShadowId", 3);
            TrackWithShadowKey(b, "ShadowId", 1);
            TrackWithShadowKey(c, "ShadowId", 2);
            await _dbContext.SaveChangesAsync();

            var repository = CreateShadowKeyRepository();
            var page1 = await repository.PagingQueryAsync(new PagingRequest(1, 10));

            Assert.Equal(3, page1.TotalCount);
            Assert.Equal(new[] { "b", "c", "a" }, page1.Data.Select(e => e.Name).ToArray());
        }

        [Fact]
        public async Task PagingQueryAsync_CompositeKeyWithShadowPart_OrdersDeterministically()
        {
            var t1id2 = new CompositeKeyEntity { Id = 2, Name = "t1id2" };
            var t1id1 = new CompositeKeyEntity { Id = 1, Name = "t1id1" };
            var t2id1 = new CompositeKeyEntity { Id = 1, Name = "t2id1" };
            TrackWithShadowKey(t1id2, "TenantId", 1);
            TrackWithShadowKey(t1id1, "TenantId", 1);
            TrackWithShadowKey(t2id1, "TenantId", 2);
            await _dbContext.SaveChangesAsync();

            var repository = CreateCompositeKeyRepository();
            var page1 = await repository.PagingQueryAsync(new PagingRequest(1, 10));

            Assert.Equal(3, page1.TotalCount);
            Assert.Equal(new[] { "t1id1", "t1id2", "t2id1" }, page1.Data.Select(e => e.Name).ToArray());
        }

        [Fact]
        public async Task PagingQueryAsync_CompositeKey_WithCallerSortOnOneKey_AppendsShadowTieBreaker()
        {
            var t1id2 = new CompositeKeyEntity { Id = 2, Name = "same" };
            var t1id1 = new CompositeKeyEntity { Id = 1, Name = "same" };
            var t2id1 = new CompositeKeyEntity { Id = 1, Name = "same" };
            TrackWithShadowKey(t1id2, "TenantId", 1);
            TrackWithShadowKey(t1id1, "TenantId", 1);
            TrackWithShadowKey(t2id1, "TenantId", 2);
            await _dbContext.SaveChangesAsync();

            var repository = CreateCompositeKeyRepository();
            var page1 = await repository.PagingQueryAsync(new PagingRequest(1, 10), e => e.Id, asc: true);

            var result = page1.Data.ToList();
            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { 1, 1, 2 }, result.Select(e => e.Id).ToArray());
        }

        private void TrackWithShadowKey<TEntity>(TEntity entity, string shadowProperty, int value)
            where TEntity : class
        {
            var entry = _dbContext.Entry(entity);
            entry.Property(shadowProperty).CurrentValue = value;
            entry.State = EntityState.Added;
        }

        private ShadowKeyPagingRepository CreateShadowKeyRepository()
        {
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            var dependencies = new EFRepositoryDependencies<TestDbContext>(
                _mockContextFactory.Object, _mockUnitOfWorkManager.Object, _mockDependenciesLogger.Object, new MiCakeEFCoreOptions(typeof(TestDbContext)));
            return new ShadowKeyPagingRepository(dependencies);
        }

        private CompositeKeyPagingRepository CreateCompositeKeyRepository()
        {
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            var dependencies = new EFRepositoryDependencies<TestDbContext>(
                _mockContextFactory.Object, _mockUnitOfWorkManager.Object, _mockDependenciesLogger.Object, new MiCakeEFCoreOptions(typeof(TestDbContext)));
            return new CompositeKeyPagingRepository(dependencies);
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

        private class TestKeylessPagingRepository : EFRepositoryHasPaging<TestDbContext, KeylessTestEntity, int>
        {
            public TestKeylessPagingRepository(
                IEFCoreContextFactory<TestDbContext> factory,
                IUnitOfWorkManager unitOfWorkManager,
                ILogger<EFRepositoryDependencies<TestDbContext>> logger)
                : base(new EFRepositoryDependencies<TestDbContext>(factory, unitOfWorkManager, logger, new MiCakeEFCoreOptions(typeof(TestDbContext))))
            {
            }
        }

        private class ShadowKeyPagingRepository : EFRepositoryHasPaging<TestDbContext, ShadowKeyEntity, int>
        {
            public ShadowKeyPagingRepository(EFRepositoryDependencies<TestDbContext> dependencies) : base(dependencies) { }
        }

        private class CompositeKeyPagingRepository : EFRepositoryHasPaging<TestDbContext, CompositeKeyEntity, int>
        {
            public CompositeKeyPagingRepository(EFRepositoryDependencies<TestDbContext> dependencies) : base(dependencies) { }
        }

        #endregion
    }

    // Move test DbContext and entity to namespace-level and make them public so Moq can proxy generic types
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities { get; set; }
        public DbSet<KeylessTestEntity> KeylessEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<KeylessTestEntity>().HasNoKey();
            modelBuilder.Entity<ShadowKeyEntity>(e =>
            {
                e.Property<int>("ShadowId").ValueGeneratedNever();
                e.HasKey("ShadowId");
            });
            modelBuilder.Entity<CompositeKeyEntity>(e =>
            {
                e.Property<int>("TenantId").ValueGeneratedNever();
                e.HasKey("TenantId", nameof(CompositeKeyEntity.Id));
            });
        }
    }

    public class TestEntity : MiCake.DDD.Domain.AggregateRoot<int>
    {
        public string Name { get; set; }
    }

    public class KeylessTestEntity : MiCake.DDD.Domain.AggregateRoot<int>
    {
        public string Name { get; set; }
    }

    public class ShadowKeyEntity : MiCake.DDD.Domain.AggregateRoot<int>
    {
        public string Name { get; set; }
    }

    public class CompositeKeyEntity : MiCake.DDD.Domain.AggregateRoot<int>
    {
        public string Name { get; set; }
    }
    }
