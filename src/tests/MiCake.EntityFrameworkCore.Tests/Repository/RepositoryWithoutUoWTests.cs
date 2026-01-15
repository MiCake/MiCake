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
    /// Unit tests for repository DbContext access without explicit Unit of Work
    /// Tests verify that repositories can fall back to DI-injected DbContext when no UoW is active
    /// </summary>
    public class RepositoryWithoutUoWTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly Mock<IUnitOfWorkManager> _mockUnitOfWorkManager;
        private readonly Mock<IEFCoreContextFactory<TestDbContext>> _mockContextFactory;
        private readonly Mock<ILogger<TestRepository>> _mockLogger;
        private readonly Mock<ILogger<EFRepositoryDependencies<TestDbContext>>> _mockDependenciesLogger;
        private readonly TestRepository _repository;

        public RepositoryWithoutUoWTests()
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

        #region Direct DbContext Access Without UoW Tests

        [Fact]
        public void DbContext_WithoutActiveUoW_ShouldFallbackToDIContainer()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = _repository.DbContext;

            // Assert
            Assert.Same(_dbContext, result);
            _mockContextFactory.Verify(f => f.GetDbContext(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetDbContextAsync_WithoutActiveUoW_ShouldReturnDbContext()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = await _repository.GetDbContextAsync();

            // Assert
            Assert.Same(_dbContext, result);
        }

        [Fact]
        public void DbSet_WithoutActiveUoW_ShouldReturnDbSet()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = _repository.DbSet;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Entities_WithoutActiveUoW_ShouldReturnQueryable()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = _repository.Entities;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void EntitiesNoTracking_WithoutActiveUoW_ShouldReturnAsNoTrackingQueryable()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = _repository.EntitiesNoTracking;

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region DbContext Caching Tests

        [Fact]
        public void DbContext_AccessedMultipleTimes_ShouldCacheSameInstance()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result1 = _repository.DbContext;
            var result2 = _repository.DbContext;
            var result3 = _repository.DbContext;

            // Assert
            Assert.Same(result1, result2);
            Assert.Same(result2, result3);
        }

        #endregion

        #region DbContext Factory Fallback Tests

        [Fact]
        public void DbContext_WithoutUoW_ShouldCallContextFactory()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act
            var result = _repository.DbContext;

            // Assert
            _mockContextFactory.Verify(f => f.GetDbContext(), Times.AtLeastOnce);
        }

        [Fact]
        public void DbContext_WithoutUoW_ShouldNotThrowWhenDbContextIsAvailable()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act & Assert
            var exception = Record.Exception(() => _repository.DbContext);
            Assert.Null(exception);
        }

        #endregion

        #region Query Operations Without UoW

        [Fact]
        public void GetAllEntities_WithoutActiveUoW_ShouldWork()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act & Assert
            var result = _repository.Entities;
            Assert.NotNull(result);
        }

        [Fact]
        public void GetNoTrackingEntities_WithoutActiveUoW_ShouldWork()
        {
            // Arrange
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);

            // Act & Assert
            var result = _repository.EntitiesNoTracking;
            Assert.NotNull(result);
        }

        #endregion

        #region Helper Classes

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        public class TestEntity : Entity<int>
        {
            public string Name { get; set; }
        }

        public class TestRepository : EFRepositoryBase<TestDbContext, TestEntity, int>
        {
            public TestRepository(EFRepositoryDependencies<TestDbContext> dependencies)
                : base(dependencies)
            {
            }

            public new TestDbContext DbContext => base.DbContext;
            public new DbSet<TestEntity> DbSet => base.DbSet;
            public new IQueryable<TestEntity> Entities => base.Entities;
            public new IQueryable<TestEntity> EntitiesNoTracking => base.EntitiesNoTracking;

            public new Task<TestDbContext> GetDbContextAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return base.GetDbContextAsync(cancellationToken);
            }
        }

        private class ObjectAccessor<T> : MiCake.Core.DependencyInjection.IObjectAccessor<T>
        {
            public ObjectAccessor(T value) => Value = value;

            public T Value { get; }
        }

        #endregion

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
