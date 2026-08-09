using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for EFCoreContextFactory.
    /// Tests DbContext creation, UoW integration, and bypass mode functionality.
    /// </summary>
    public class EFCoreContextFactoryTests : IDisposable
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IUnitOfWorkManager> _mockUnitOfWorkManager;
        private readonly Mock<ILogger<EFCoreContextFactory<TestFactoryDbContext>>> _mockLogger;
        private readonly Mock<ILogger<EFCoreDbContextWrapper>> _mockWrapperLogger;
        private readonly TestFactoryDbContext _dbContext;

        public EFCoreContextFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockUnitOfWorkManager = new Mock<IUnitOfWorkManager>();
            _mockLogger = new Mock<ILogger<EFCoreContextFactory<TestFactoryDbContext>>>();
            _mockWrapperLogger = new Mock<ILogger<EFCoreDbContextWrapper>>();

            var options = new DbContextOptionsBuilder<TestFactoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new TestFactoryDbContext(options);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(TestFactoryDbContext)))
                .Returns(_dbContext);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<EFCoreDbContextWrapper>)))
                .Returns(_mockWrapperLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            // Act
            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreContextFactory<TestFactoryDbContext>(
                    null!,
                    _mockUnitOfWorkManager.Object,
                    _mockLogger.Object,
                    optionsAccessor));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWorkManager_ShouldThrowArgumentNullException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreContextFactory<TestFactoryDbContext>(
                    _mockServiceProvider.Object,
                    null!,
                    _mockLogger.Object,
                    optionsAccessor));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreContextFactory<TestFactoryDbContext>(
                    _mockServiceProvider.Object,
                    _mockUnitOfWorkManager.Object,
                    null!,
                    optionsAccessor));
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EFCoreContextFactory<TestFactoryDbContext>(
                    _mockServiceProvider.Object,
                    _mockUnitOfWorkManager.Object,
                    _mockLogger.Object,
                    null!));
        }

        #endregion

        #region GetDbContext Tests

        [Fact]
        public void GetDbContext_WithActiveUoW_ShouldReturnDbContext()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            mockUow.As<IUnitOfWorkInternal>();
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var result = factory.GetDbContext();

            // Assert
            Assert.NotNull(result);
            Assert.Same(_dbContext, result);
        }

        [Fact]
        public void GetDbContext_WithoutUoW_WhenDisallowed_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                AllowDbContextAccessWithoutUoW = false
            };
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null!);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetDbContext());
            Assert.Contains("No active Unit of Work", exception.Message);
        }

        [Fact]
        public void GetDbContext_WithoutUoW_WhenAllowed_ShouldReturnDbContext()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
            };
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null!);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var result = factory.GetDbContext();

            // Assert
            Assert.NotNull(result);
            Assert.Same(_dbContext, result);
        }

        [Fact]
        public void GetDbContext_WhenDbContextNotRegistered_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
            };
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null!);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(TestFactoryDbContext)))
                .Throws(new InvalidOperationException("DbContext not registered"));

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetDbContext());
            Assert.Contains("Failed to resolve", exception.Message);
        }

        #endregion

        #region GetOrCreateWrapperFor Tests

        [Fact]
        public void GetOrCreateWrapperFor_WithActiveUoW_ShouldReturnWrapper()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            mockUow.As<IUnitOfWorkInternal>();
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var result = factory.GetOrCreateWrapperFor(_dbContext);

            // Assert
            Assert.NotNull(result);
            Assert.Same(_dbContext, result.DbContext);
        }

        [Fact]
        public void GetOrCreateWrapperFor_WithActiveUoW_ShouldRegisterWithUoW()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var mockInternalUow = mockUow.As<IUnitOfWorkInternal>();
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            factory.GetOrCreateWrapperFor(_dbContext);

            // Assert
            mockInternalUow.Verify(u => u.RegisterResource(It.IsAny<EFCoreDbContextWrapper>()), Times.Once);
        }

        [Fact]
        public void GetOrCreateWrapperFor_WithUoWNotImplementingInternal_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            // Mock UoW that does NOT implement IUnitOfWorkInternal
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(mockUow.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act & Assert - a live UoW that cannot own resources fails fast instead of
            // returning an unregistered wrapper
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetOrCreateWrapperFor(_dbContext));
            Assert.Contains("IUnitOfWorkInternal", exception.Message);
        }

        [Fact]
        public void GetOrCreateWrapperFor_WithoutUoW_WhenAllowed_ShouldLogWarning()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
            };
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null!);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            factory.GetOrCreateWrapperFor(_dbContext);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AllowDbContextAccessWithoutUoW")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Frame-Stable Identity and Ownership Tests

        [Fact]
        public void GetOrCreateWrapperFor_SameRootTwice_ShouldReturnSameWrapper()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            var rootUow = CreateMockRootUow(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(rootUow.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var first = factory.GetOrCreateWrapperFor(_dbContext);
            var second = factory.GetOrCreateWrapperFor(_dbContext);

            // Assert - the same ambient frame and DbContext type resolve the same instance
            Assert.Same(first, second);
        }

        [Fact]
        public void GetOrCreateWrapperFor_NestedUow_ShouldShareRootWrapper()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            var rootUow = CreateMockRootUow(Guid.NewGuid());
            var nestedUow = CreateMockNestedUow(Guid.NewGuid(), rootUow.Object);
            _mockUnitOfWorkManager.SetupSequence(um => um.Current)
                .Returns(rootUow.Object)
                .Returns(nestedUow.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var rootWrapper = factory.GetOrCreateWrapperFor(_dbContext);
            var nestedWrapper = factory.GetOrCreateWrapperFor(_dbContext);

            // Assert - shared nested UoWs share the root identity and therefore the wrapper
            Assert.Same(rootWrapper, nestedWrapper);
        }

        [Fact]
        public void GetOrCreateWrapperFor_DifferentLiveRoot_ShouldRejectReuse()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            var root1 = CreateMockRootUow(Guid.NewGuid());
            var root2 = CreateMockRootUow(Guid.NewGuid());
            _mockUnitOfWorkManager.SetupSequence(um => um.Current)
                .Returns(root1.Object)
                .Returns(root2.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            factory.GetOrCreateWrapperFor(_dbContext);

            // Act & Assert - a context bound to live root1 cannot be reused by live root2
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetOrCreateWrapperFor(_dbContext));
            Assert.Contains("already bound", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ExecuteRequiresNewAsync", exception.Message);
        }

        [Fact]
        public void GetOrCreateWrapperFor_CompletedRoot_ShouldCreateNewWrapper()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            var completedRoot = CreateMockRootUow(Guid.NewGuid());
            completedRoot.Setup(r => r.IsCompleted).Returns(true);

            var newRoot = CreateMockRootUow(Guid.NewGuid());
            _mockUnitOfWorkManager.SetupSequence(um => um.Current)
                .Returns(completedRoot.Object)
                .Returns(newRoot.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            factory.GetOrCreateWrapperFor(_dbContext);

            // Act - completed root's wrapper is stale; a new live root gets a fresh wrapper
            var freshWrapper = factory.GetOrCreateWrapperFor(_dbContext);

            // Assert
            Assert.NotNull(freshWrapper);
        }

        private static Mock<IUnitOfWork> CreateMockRootUow(Guid id)
        {
            var mock = new Mock<IUnitOfWork>();
            mock.Setup(u => u.Id).Returns(id);
            mock.Setup(u => u.Parent).Returns((IUnitOfWork?)null);
            mock.Setup(u => u.IsCompleted).Returns(false);
            mock.Setup(u => u.IsDisposed).Returns(false);
            mock.As<IUnitOfWorkInternal>();
            return mock;
        }

        private static Mock<IUnitOfWork> CreateMockNestedUow(Guid id, IUnitOfWork parent)
        {
            var mock = new Mock<IUnitOfWork>();
            mock.Setup(u => u.Id).Returns(id);
            mock.Setup(u => u.Parent).Returns(parent);
            mock.Setup(u => u.IsCompleted).Returns(false);
            mock.Setup(u => u.IsDisposed).Returns(false);
            mock.As<IUnitOfWorkInternal>();
            return mock;
        }

        [Fact]
        public void GetOrCreateWrapperFor_WhenRootAlreadyBoundToDifferentContext_ShouldReject()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext));
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            var rootUow = CreateMockRootUow(Guid.NewGuid());
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns(rootUow.Object);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            var otherOptions = new DbContextOptionsBuilder<TestFactoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var otherContext = new TestFactoryDbContext(otherOptions);

            // Act - the first write anchors the wrapper to the resolved context instance
            var first = factory.GetOrCreateWrapperFor(_dbContext);

            // Assert - a different context instance for the same root is rejected instead
            // of silently returning the cached wrapper bound to another instance
            var exception = Assert.Throws<InvalidOperationException>(
                () => factory.GetOrCreateWrapperFor(otherContext));
            Assert.Contains("different context instance", exception.Message);
            Assert.Same(_dbContext, first.DbContext);
        }

        #endregion

        #region Error Message Quality Tests

        [Fact]
        public void GetDbContext_ErrorMessage_ShouldContainDbContextTypeName()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                AllowDbContextAccessWithoutUoW = false
            };
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null!);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetDbContext());

            // Assert
            Assert.Contains(nameof(TestFactoryDbContext), exception.Message);
            Assert.Contains("unitOfWorkManager.BeginAsync()", exception.Message);
        }

        [Fact]
        public void GetDbContext_ErrorMessage_ShouldSuggestAccessOption()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                AllowDbContextAccessWithoutUoW = false
            };
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null!);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => factory.GetDbContext());

            // Assert
            Assert.Contains(nameof(MiCakeEFCoreOptions), exception.Message);
            Assert.Contains(nameof(MiCakeEFCoreOptions.AllowDbContextAccessWithoutUoW), exception.Message);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Integration_GetDbContext_WithRealDI_ShouldWork()
        {
            // Arrange
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestFactoryDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddScoped(typeof(IEFCoreContextFactory<TestFactoryDbContext>), typeof(EFCoreContextFactory<TestFactoryDbContext>));

            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                AllowDbContextAccessWithoutUoW = true
            };
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(efCoreOptions);

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IEFCoreContextFactory<TestFactoryDbContext>>();

            // Act
            var dbContext = factory.GetDbContext();

            // Assert
            Assert.NotNull(dbContext);
            var count = await dbContext.TestEntities.CountAsync();
            Assert.Equal(0, count);
        }

        #endregion

        #region Helper Methods

        private IObjectAccessor<MiCakeEFCoreOptions> CreateOptionsAccessor(MiCakeEFCoreOptions options)
        {
            return new ObjectAccessor<MiCakeEFCoreOptions>(options);
        }

        private class ObjectAccessor<T> : IObjectAccessor<T>
        {
            public ObjectAccessor(T value) => Value = value;
            public T Value { get; }
        }

        #endregion

        #region Helper Classes

        public class TestFactoryDbContext : DbContext
        {
            public TestFactoryDbContext(DbContextOptions<TestFactoryDbContext> options) : base(options) { }

            public DbSet<TestFactoryEntity> TestEntities { get; set; } = null!;
        }

        public class TestFactoryEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        #endregion

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
