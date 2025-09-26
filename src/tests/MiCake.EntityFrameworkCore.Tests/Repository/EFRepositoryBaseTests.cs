using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore.Tests.Seed;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Repository
{
    /// <summary>
    /// Comprehensive unit tests for EFRepositoryBase to ensure correct behavior
    /// with both explicit and implicit Unit of Work modes
    /// </summary>
    public class EFRepositoryBaseTests : IDisposable
    {
        private readonly IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private readonly Mock<IUnitOfWorkManager> _mockUowManager;
        private readonly Mock<IEFCoreContextFactory<TestDbContext>> _mockContextFactory;
        private readonly Mock<ILogger<TestRepository>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly TestDbContext _testDbContext;

        public EFRepositoryBaseTests()
        {
            _services = new ServiceCollection();
            _mockUowManager = new Mock<IUnitOfWorkManager>();
            _mockContextFactory = new Mock<IEFCoreContextFactory<TestDbContext>>();
            _mockLogger = new Mock<ILogger<TestRepository>>();
            _mockUow = new Mock<IUnitOfWork>();

            // Set up test DbContext
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _testDbContext = new TestDbContext(options);

            SetupServices();
        }

        private void SetupServices()
        {
            _services.AddSingleton(_mockUowManager.Object);
            _services.AddSingleton(_mockContextFactory.Object);
            _services.AddSingleton<ILogger<EFRepositoryBase<TestDbContext, TestEntity, int>>>(_mockLogger.Object);
            _services.AddTransient<TestRepository>();

            // Setup mock context factory to return our test context
            _mockContextFactory
                .Setup(x => x.GetDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testDbContext);

            // Setup mock UoW with a test ID
            _mockUow.Setup(x => x.Id).Returns(Guid.NewGuid());
        }

        private IServiceProvider BuildServiceProvider(MiCakeEFCoreOptions options)
        {
            _services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(
                new ObjectAccessor<MiCakeEFCoreOptions>(options));

            return _services.BuildServiceProvider();
        }

        #region Explicit UoW Mode Tests

        [Fact]
        public void ExplicitMode_WithActiveUoW_ShouldAllowDbContextAccess()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = false };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns(_mockUow.Object);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var dbContext = repository.TestDbContext; // Should not throw
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void ExplicitMode_WithoutActiveUoW_ShouldThrowException()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = false };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => repository.TestDbContext);
            Assert.Contains("Cannot access TestDbContext outside of a Unit of Work scope", exception.Message);
            Assert.Contains("Please wrap your operation in: using var uow = unitOfWorkManager.Begin()", exception.Message);
            Assert.Contains("Or enable ImplicitModeForUow in MiCakeEFCoreOptions", exception.Message);
        }

        [Fact]
        public void ExplicitMode_WithoutActiveUoW_DbSetAccess_ShouldThrowException()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = false };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => repository.TestDbSet);
            Assert.Contains("Cannot access TestDbContext outside of a Unit of Work scope", exception.Message);
        }

        [Fact]
        public void ExplicitMode_WithoutActiveUoW_EntitiesAccess_ShouldThrowException()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = false };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => repository.TestEntities);
            Assert.Contains("Cannot access TestDbContext outside of a Unit of Work scope", exception.Message);
        }

        #endregion

        #region Implicit UoW Mode Tests

        [Fact]
        public void ImplicitMode_WithoutActiveUoW_ShouldAllowDbContextAccess()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var dbContext = repository.TestDbContext; // Should not throw
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void ImplicitMode_WithActiveUoW_ShouldStillWork()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns(_mockUow.Object);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var dbContext = repository.TestDbContext; // Should not throw
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void ImplicitMode_WithoutActiveUoW_DbSetAccess_ShouldWork()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var dbSet = repository.TestDbSet; // Should not throw
            Assert.NotNull(dbSet);
        }

        [Fact]
        public void ImplicitMode_WithoutActiveUoW_EntitiesAccess_ShouldWork()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var entities = repository.TestEntities; // Should not throw
            Assert.NotNull(entities);
        }

        [Fact]
        public void ImplicitMode_WithoutActiveUoW_EntitiesNoTrackingAccess_ShouldWork()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var entities = repository.TestEntitiesNoTracking; // Should not throw
            Assert.NotNull(entities);
        }

        #endregion

        #region Caching Behavior Tests

        [Fact]
        public void Caching_WithSameUoW_ShouldReturnCachedDbContext()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = false };
            _serviceProvider = BuildServiceProvider(options);

            var uowId = Guid.NewGuid();
            _mockUow.Setup(x => x.Id).Returns(uowId);
            _mockUowManager.Setup(x => x.Current).Returns(_mockUow.Object);

            var repository = new TestRepository(_serviceProvider);

            // Act
            var dbContext1 = repository.TestDbContext;
            var dbContext2 = repository.TestDbContext;

            // Assert
            Assert.Same(dbContext1, dbContext2);
            // Context factory should only be called once due to caching
            _mockContextFactory.Verify(x => x.GetDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Caching_WithDifferentUoW_ShouldInvalidateCache()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = false };
            _serviceProvider = BuildServiceProvider(options);

            var uowId1 = Guid.NewGuid();
            var uowId2 = Guid.NewGuid();
            var mockUow1 = new Mock<IUnitOfWork>();
            var mockUow2 = new Mock<IUnitOfWork>();
            mockUow1.Setup(x => x.Id).Returns(uowId1);
            mockUow2.Setup(x => x.Id).Returns(uowId2);

            var repository = new TestRepository(_serviceProvider);

            // Act
            _mockUowManager.Setup(x => x.Current).Returns(mockUow1.Object);
            var dbContext1 = repository.TestDbContext;

            _mockUowManager.Setup(x => x.Current).Returns(mockUow2.Object);
            var dbContext2 = repository.TestDbContext;

            // Assert
            Assert.Same(dbContext1, dbContext2); // Same because our mock returns same instance
            // But factory should be called twice due to UoW change
            _mockContextFactory.Verify(x => x.GetDbContextAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public void Caching_ImplicitModeWithoutUoW_ShouldUseSeparateCache()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act
            var dbContext1 = repository.TestDbContext;
            var dbContext2 = repository.TestDbContext;

            // Assert
            Assert.Same(dbContext1, dbContext2);
            // Context factory should only be called once due to caching
            _mockContextFactory.Verify(x => x.GetDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Caching_TransitionFromUoWToImplicitMode_ShouldInvalidateCache()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            var repository = new TestRepository(_serviceProvider);

            // Act
            // First access with UoW
            _mockUowManager.Setup(x => x.Current).Returns(_mockUow.Object);
            var dbContext1 = repository.TestDbContext;

            // Second access without UoW (implicit mode)
            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);
            var dbContext2 = repository.TestDbContext;

            // Assert
            Assert.Same(dbContext1, dbContext2); // Same because mock returns same instance
            // Factory should be called twice due to cache invalidation
            _mockContextFactory.Verify(x => x.GetDbContextAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        #endregion

        #region Async Method Tests

        [Fact]
        public async Task GetDbContextAsync_ShouldDelegateToContextFactory()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            var repository = new TestRepository(_serviceProvider);
            var cancellationToken = new CancellationToken();

            // Act
            var dbContext = await repository.TestGetDbContextAsync(cancellationToken);

            // Assert
            Assert.NotNull(dbContext);
            _mockContextFactory.Verify(x => x.GetDbContextAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetDbSetAsync_ShouldReturnCorrectDbSet()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            var repository = new TestRepository(_serviceProvider);

            // Act
            var dbSet = await repository.TestGetDbSetAsync();

            // Assert
            Assert.NotNull(dbSet);
            Assert.IsAssignableFrom<DbSet<TestEntity>>(dbSet);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task Caching_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act
            var tasks = new Task<TestDbContext>[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => repository.TestDbContext);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            // All results should be the same cached instance
            for (int i = 1; i < results.Length; i++)
            {
                Assert.Same(results[0], results[i]);
            }

            // Context factory should be called only once despite concurrent access
            _mockContextFactory.Verify(x => x.GetDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void ContextFactoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            var expectedException = new InvalidOperationException("Factory error for testing");
            _mockContextFactory
                .Setup(x => x.GetDbContextAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var actualException = Assert.Throws<InvalidOperationException>(() => repository.TestDbContext);
            Assert.Contains("Failed to create a DbContext for TestDbContext", actualException.Message);
            Assert.Same(expectedException, actualException.InnerException);
        }

        [Fact]
        public void ExplicitMode_NoActiveUoW_ShouldThrowHelpfulException()
        {
            // Arrange - explicit mode requires UoW
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = false };
            _serviceProvider = BuildServiceProvider(options);

            // No active UoW
            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = new TestRepository(_serviceProvider);

            // Act & Assert
            var actualException = Assert.Throws<InvalidOperationException>(() => repository.TestDbContext);
            Assert.Contains("Cannot access TestDbContext outside of a Unit of Work scope", actualException.Message);
            Assert.Contains("Please wrap your operation in: using var uow = unitOfWorkManager.Begin()", actualException.Message);
            Assert.Contains("Or enable ImplicitModeForUow in MiCakeEFCoreOptions", actualException.Message);
        }

        #endregion

        public void Dispose()
        {
            _testDbContext?.Dispose();
            _serviceProvider?.GetService<ServiceProvider>()?.Dispose();
        }

        #region Test Classes

        /// <summary>
        /// Test DbContext for repository testing
        /// </summary>
        public class TestDbContext : MiCakeDbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<TestEntity>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Name).HasMaxLength(100);
                });
            }
        }

        /// <summary>
        /// Test entity for repository testing
        /// </summary>
        public class TestEntity : IEntity<int>
        {
            private readonly List<IDomainEvent> _domainEvents = new List<IDomainEvent>();

            public int Id { get; set; }
            public string Name { get; set; }

            public void AddDomainEvent(IDomainEvent domainEvent)
            {
                _domainEvents.Add(domainEvent);
            }

            public void RemoveDomainEvent(IDomainEvent domainEvent)
            {
                _domainEvents.Remove(domainEvent);
            }

            public List<IDomainEvent> GetDomainEvents()
            {
                return _domainEvents;
            }
        }

        /// <summary>
        /// Test repository implementation for testing EFRepositoryBase
        /// </summary>
        public class TestRepository : EFRepositoryBase<TestDbContext, TestEntity, int>
        {
            public TestRepository(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            // Expose protected members for testing
            public TestDbContext TestDbContext => DbContext;
            public DbSet<TestEntity> TestDbSet => DbSet;
            public IQueryable<TestEntity> TestEntities => Entities;
            public IQueryable<TestEntity> TestEntitiesNoTracking => EntitiesNoTracking;

            public Task<TestDbContext> TestGetDbContextAsync(CancellationToken cancellationToken = default)
                => GetDbContextAsync(cancellationToken);

            public Task<DbSet<TestEntity>> TestGetDbSetAsync(CancellationToken cancellationToken = default)
                => GetDbSetAsync(cancellationToken);
        }

        /// <summary>
        /// Simple object accessor implementation for testing
        /// </summary>
        private class ObjectAccessor<T> : IObjectAccessor<T>
        {
            public T Value { get; }

            public ObjectAccessor(T value)
            {
                Value = value;
            }
        }

        #endregion
    }
}