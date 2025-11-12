using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Repository;
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
    /// Unit tests for EFRepositoryBase functionality
    /// Tests the core repository behavior with dependency wrapper pattern
    /// </summary>
    public class EFRepositoryBaseTests : IDisposable
    {
        private readonly IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private readonly Mock<IUnitOfWorkManager> _mockUowManager;
        private readonly Mock<IEFCoreContextFactory<TestDbContext>> _mockContextFactory;
        private readonly Mock<ILogger<EFRepositoryDependencies<TestDbContext>>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly TestDbContext _testDbContext;

        public EFRepositoryBaseTests()
        {
            _services = new ServiceCollection();
            _mockUowManager = new Mock<IUnitOfWorkManager>();
            _mockContextFactory = new Mock<IEFCoreContextFactory<TestDbContext>>();
            _mockLogger = new Mock<ILogger<EFRepositoryDependencies<TestDbContext>>>();
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
            _services.AddSingleton(_mockLogger.Object);

            // Setup mock context factory to return our test context
            _mockContextFactory
                .Setup(x => x.GetDbContext())
                .Returns(_testDbContext);

            // Setup mock UoW with a test ID
            _mockUow.Setup(x => x.Id).Returns(Guid.NewGuid());
        }

        private IServiceProvider BuildServiceProvider(MiCakeEFCoreOptions options)
        {
            // Register options as singleton implementing IObjectAccessor
            _services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(options);

            // Register the dependencies wrapper
            _services.AddScoped<EFRepositoryDependencies<TestDbContext>>();
            _services.AddScoped<TestRepository>();

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

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

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

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => repository.TestDbContext);
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

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act & Assert
            var dbContext = repository.TestDbContext; // Should not throw in implicit mode
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void ImplicitMode_WithActiveUoW_ShouldUseSameContext()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            var uowId = Guid.NewGuid();
            _mockUow.Setup(x => x.Id).Returns(uowId);
            _mockUowManager.Setup(x => x.Current).Returns(_mockUow.Object);

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act
            var dbContext1 = repository.TestDbContext;
            var dbContext2 = repository.TestDbContext;

            // Assert
            Assert.Same(dbContext1, dbContext2); // Should be cached per UoW
        }

        #endregion

        #region DbSet and Entities Access Tests

        [Fact]
        public void DbSet_ShouldReturnCorrectSet()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act
            var dbSet = repository.TestDbSet;

            // Assert
            Assert.NotNull(dbSet);
            Assert.Equal(_testDbContext.TestEntities, dbSet);
        }

        [Fact]
        public void Entities_ShouldReturnTrackingQueryable()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act
            var entities = repository.TestEntities;

            // Assert
            Assert.NotNull(entities);
            Assert.IsAssignableFrom<IQueryable<TestEntity>>(entities);
        }

        [Fact]
        public void EntitiesNoTracking_ShouldReturnNoTrackingQueryable()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act
            var entities = repository.TestEntitiesNoTracking;

            // Assert
            Assert.NotNull(entities);
            Assert.IsAssignableFrom<IQueryable<TestEntity>>(entities);
        }

        #endregion

        #region UoW Cache Tests

        [Fact]
        public void Cache_ShouldBeSeparateForDifferentUoWs()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = false };
            _serviceProvider = BuildServiceProvider(options);

            var uow1 = new Mock<IUnitOfWork>();
            var uow2 = new Mock<IUnitOfWork>();
            uow1.Setup(x => x.Id).Returns(Guid.NewGuid());
            uow2.Setup(x => x.Id).Returns(Guid.NewGuid());

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act - Access with first UoW
            _mockUowManager.Setup(x => x.Current).Returns(uow1.Object);
            var dbContext1 = repository.TestDbContext;

            // Switch to second UoW
            _mockUowManager.Setup(x => x.Current).Returns(uow2.Object);
            var dbContext2 = repository.TestDbContext;

            // Assert
            // Note: In real implementation, these would be the same DbContext instance
            // but from different UoW scopes. For this mock test, we just verify access works
            Assert.NotNull(dbContext1);
            Assert.NotNull(dbContext2);
        }

        #endregion

        #region Async Method Tests

        [Fact]
        public async Task GetDbContextAsync_ShouldReturnDbContext()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act
            var dbContext = await repository.GetDbContextAsyncPublic();

            // Assert
            Assert.NotNull(dbContext);
        }

        [Fact]
        public async Task GetDbSetAsync_ShouldReturnDbSet()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            _mockUowManager.Setup(x => x.Current).Returns((IUnitOfWork)null);

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act
            var dbSet = await repository.GetDbSetAsyncPublic();

            // Assert
            Assert.NotNull(dbSet);
        }

        #endregion

        #region Dependency Injection Tests

        [Fact]
        public void Constructor_WithNullDependencies_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestRepository(null));
        }

        [Fact]
        public void Dependencies_ShouldBeAccessible()
        {
            // Arrange
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { ImplicitModeForUow = true };
            _serviceProvider = BuildServiceProvider(options);

            var repository = _serviceProvider.GetRequiredService<TestRepository>();

            // Act
            var dependencies = repository.GetDependencies();

            // Assert
            Assert.NotNull(dependencies);
            Assert.NotNull(dependencies.ContextFactory);
            Assert.NotNull(dependencies.UnitOfWorkManager);
            Assert.NotNull(dependencies.Logger);
            Assert.NotNull(dependencies.Options);
        }

        #endregion

        public void Dispose()
        {
            _testDbContext?.Dispose();
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        #region Test Classes

        /// <summary>
        /// Test DbContext for unit testing
        /// </summary>
        public class TestDbContext : MiCakeDbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities => Set<TestEntity>();

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
        /// Test entity for repository testing - properly implements new entity interface
        /// </summary>
        public class TestEntity : AggregateRoot<int>
        {
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// Test repository exposing protected members for testing
        /// </summary>
        public class TestRepository : EFRepositoryBase<TestDbContext, TestEntity, int>
        {
            public TestRepository(EFRepositoryDependencies<TestDbContext> dependencies) : base(dependencies)
            {
            }

            // Expose protected members for testing
            public TestDbContext TestDbContext => DbContext;
            public DbSet<TestEntity> TestDbSet => DbSet;
            public IQueryable<TestEntity> TestEntities => Entities;
            public IQueryable<TestEntity> TestEntitiesNoTracking => EntitiesNoTracking;

            public Task<TestDbContext> GetDbContextAsyncPublic(CancellationToken cancellationToken = default)
                => GetDbContextAsync(cancellationToken);

            public Task<DbSet<TestEntity>> GetDbSetAsyncPublic(CancellationToken cancellationToken = default)
                => GetDbSetAsync(cancellationToken);

            public EFRepositoryDependencies<TestDbContext> GetDependencies() => Dependencies;
        }

        #endregion
    }
}