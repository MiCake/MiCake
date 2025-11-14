using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Repository
{
    /// <summary>
    /// Unit tests for EFRepositoryBase
    /// Tests the base repository functionality including UoW-aware caching and DbContext management
    /// </summary>
    public class EFRepositoryBaseTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly Mock<IUnitOfWorkManager> _mockUnitOfWorkManager;
        private readonly Mock<IEFCoreContextFactory<TestDbContext>> _mockContextFactory;
        private readonly Mock<ILogger<TestRepository>> _mockLogger;
        private readonly Mock<ILogger<EFRepositoryDependencies<TestDbContext>>> _mockDependenciesLogger;
        private readonly TestRepository _repository;

        public EFRepositoryBaseTests()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new TestDbContext(options);
            _mockUnitOfWorkManager = new Mock<IUnitOfWorkManager>();
            _mockContextFactory = new Mock<IEFCoreContextFactory<TestDbContext>>();
            _mockLogger = new Mock<ILogger<TestRepository>>();
            _mockDependenciesLogger = new Mock<ILogger<EFRepositoryDependencies<TestDbContext>>>();

            var optionsAccessor = new ObjectAccessor<MiCakeEFCoreOptions>(new MiCakeEFCoreOptions(typeof(TestDbContext)));

            var dependencies = new EFRepositoryDependencies<TestDbContext>(
                _mockContextFactory.Object,
                _mockUnitOfWorkManager.Object,
                _mockDependenciesLogger.Object,
                optionsAccessor);

            _repository = new TestRepository(dependencies);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullDependencies_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestRepository(null));
        }

        #endregion

        #region DbContext Property Tests

        [Fact]
        public void DbContext_WithinUowScope_ShouldReturnDbContextFromFactory()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = _repository.DbContext;

            // Assert
            Assert.Same(_dbContext, result);
            _mockContextFactory.Verify(f => f.GetDbContext(), Times.Once);
        }

        [Fact]
        public void DbContext_OutsideUowScope_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _repository.DbContext);
            Assert.Contains("outside of a Unit of Work scope", exception.Message);
        }

        [Fact]
        public void DbContext_MultipleAccessesWithinSameUow_ShouldReuseCachedContext()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var context1 = _repository.DbContext;
            var context2 = _repository.DbContext;
            var context3 = _repository.DbContext;

            // Assert
            Assert.Same(_dbContext, context1);
            Assert.Same(_dbContext, context2);
            Assert.Same(_dbContext, context3);

            // Factory should only be called once due to caching
            _mockContextFactory.Verify(f => f.GetDbContext(), Times.Once);
        }

        #endregion

        #region DbSet Property Tests

        [Fact]
        public void DbSet_WithinUowScope_ShouldReturnCorrectDbSet()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var dbSet = _repository.DbSet;

            // Assert
            Assert.NotNull(dbSet);
            Assert.Equal(_dbContext.Set<TestEntity>(), dbSet);
        }

        #endregion

        #region Queryable Properties Tests

        [Fact]
        public void Entities_WithTracking_ShouldReturnQueryableWithTracking()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var entities = _repository.Entities;

            // Assert
            Assert.NotNull(entities);
            Assert.IsAssignableFrom<IQueryable<TestEntity>>(entities);
        }

        [Fact]
        public void EntitiesNoTracking_ShouldReturnQueryableWithoutTracking()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var entities = _repository.EntitiesNoTracking;

            // Assert
            Assert.NotNull(entities);
            Assert.IsAssignableFrom<IQueryable<TestEntity>>(entities);
        }

        #endregion

        #region Async Methods Tests

        [Fact]
        public async Task GetDbContextAsync_ShouldReturnDbContext()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = await _repository.GetDbContextAsync();

            // Assert
            Assert.Same(_dbContext, result);
        }

        [Fact]
        public async Task GetDbSetAsync_ShouldReturnDbSet()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = await _repository.GetDbSetAsync();

            // Assert
            Assert.Equal(_dbContext.Set<TestEntity>(), result);
        }

        #endregion

        #region UoW Caching Tests

        [Fact]
        public void UowCaching_DifferentUowIds_ShouldCreateSeparateCacheContexts()
        {
            // Arrange
            var uow1 = new Mock<IUnitOfWork>();
            uow1.Setup(u => u.Id).Returns(Guid.NewGuid());

            var uow2 = new Mock<IUnitOfWork>();
            uow2.Setup(u => u.Id).Returns(Guid.NewGuid());

            var dbContext1 = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("db1").Options);
            var dbContext2 = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("db2").Options);

            _mockContextFactory.SetupSequence(f => f.GetDbContext())
                .Returns(dbContext1)
                .Returns(dbContext2);

            // Act: Access with first UoW
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(uow1.Object);
            var context1 = _repository.DbContext;

            // Act: Switch to second UoW
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(uow2.Object);
            var context2 = _repository.DbContext;

            // Assert: Different contexts for different UoWs
            Assert.Same(dbContext1, context1);
            Assert.Same(dbContext2, context2);
            Assert.NotSame(context1, context2);

            // Cleanup
            dbContext1.Dispose();
            dbContext2.Dispose();
        }

        [Fact]
        public void UowCaching_SameUowId_ShouldReuseCacheContext()
        {
            // Arrange
            var uowId = Guid.NewGuid();
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(uowId);
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act: Multiple accesses with same UoW
            var context1 = _repository.DbContext;
            var context2 = _repository.DbContext;
            var dbSet1 = _repository.DbSet;
            var dbSet2 = _repository.DbSet;

            // Assert: Same instances returned
            Assert.Same(_dbContext, context1);
            Assert.Same(_dbContext, context2);
            Assert.Same(dbSet1, dbSet2);

            // Factory should only be called once
            _mockContextFactory.Verify(f => f.GetDbContext(), Times.Once);
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public async Task IntegrationScenario_CrudOperationsWithinUow_ShouldWorkCorrectly()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act: Perform CRUD operations
            var entity = new TestEntity { Name = "Test Entity" };
            _repository.DbSet.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Assert: Entity was added
            var savedEntity = await _repository.Entities.FirstOrDefaultAsync(e => e.Id == entity.Id);
            Assert.NotNull(savedEntity);
            Assert.Equal("Test Entity", savedEntity.Name);
        }

        [Fact]
        public async Task IntegrationScenario_QueryOperations_ShouldUseCorrectQueryables()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Add test data
            var entity1 = new TestEntity { Name = "Entity 1" };
            var entity2 = new TestEntity { Name = "Entity 2" };
            _repository.DbSet.AddRange(entity1, entity2);
            await _dbContext.SaveChangesAsync();

            // Act: Query using both tracking and non-tracking
            var trackedEntities = await _repository.Entities.ToListAsync();
            var nonTrackedEntities = await _repository.EntitiesNoTracking.ToListAsync();

            // Assert: Both should return same data
            Assert.Equal(2, trackedEntities.Count);
            Assert.Equal(2, nonTrackedEntities.Count);
            Assert.Equal(trackedEntities.Select(e => e.Name).OrderBy(n => n),
                        nonTrackedEntities.Select(e => e.Name).OrderBy(n => n));
        }

        #endregion

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #region Test Classes

        private class ObjectAccessor<T> : MiCake.Core.DependencyInjection.IObjectAccessor<T>
        {
            public ObjectAccessor(T value)
            {
                Value = value;
            }

            public T Value { get; }
        }

        private class TestRepository : EFRepositoryBase<TestDbContext, TestEntity, int>
        {
            public TestRepository(EFRepositoryDependencies<TestDbContext> dependencies)
                : base(dependencies)
            {
            }

            // Expose protected properties for testing
            public new TestDbContext DbContext => base.DbContext;
            public new DbSet<TestEntity> DbSet => base.DbSet;
            public new IQueryable<TestEntity> Entities => base.Entities;
            public new IQueryable<TestEntity> EntitiesNoTracking => base.EntitiesNoTracking;

            // Expose protected methods for testing
            public new Task<TestDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
                => base.GetDbContextAsync(cancellationToken);

            public new Task<DbSet<TestEntity>> GetDbSetAsync(CancellationToken cancellationToken = default)
                => base.GetDbSetAsync(cancellationToken);
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        public class TestEntity : Entity<int>
        {
            public string Name { get; set; }
        }

        #endregion
    }
}