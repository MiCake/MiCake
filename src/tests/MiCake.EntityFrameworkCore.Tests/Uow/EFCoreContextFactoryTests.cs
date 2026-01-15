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
        public void GetDbContext_WithoutUoW_WhenBypassDisabled_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                BypassUnitOfWorkCheck = false
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
        public void GetDbContext_WithoutUoW_WhenBypassEnabled_ShouldReturnDbContext()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                BypassUnitOfWorkCheck = true
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
                BypassUnitOfWorkCheck = true
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

        #region GetDbContextWrapper Tests

        [Fact]
        public void GetDbContextWrapper_WithActiveUoW_ShouldReturnWrapper()
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
            var result = factory.GetDbContextWrapper();

            // Assert
            Assert.NotNull(result);
            Assert.Same(_dbContext, result.DbContext);
        }

        [Fact]
        public void GetDbContextWrapper_WithActiveUoW_ShouldRegisterWithUoW()
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
            factory.GetDbContextWrapper();

            // Assert
            mockInternalUow.Verify(u => u.RegisterResource(It.IsAny<EFCoreDbContextWrapper>()), Times.Once);
        }

        [Fact]
        public void GetDbContextWrapper_WithUoWNotImplementingInternal_ShouldLogWarning()
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

            // Act
            var wrapper = factory.GetDbContextWrapper();

            // Assert
            Assert.NotNull(wrapper);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("IUnitOfWorkInternal")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetDbContextWrapper_WithoutUoW_WhenBypassEnabled_ShouldLogWarning()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                BypassUnitOfWorkCheck = true
            };
            var optionsAccessor = CreateOptionsAccessor(efCoreOptions);

            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null!);

            var factory = new EFCoreContextFactory<TestFactoryDbContext>(
                _mockServiceProvider.Object,
                _mockUnitOfWorkManager.Object,
                _mockLogger.Object,
                optionsAccessor);

            // Act
            factory.GetDbContextWrapper();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("BypassUnitOfWorkCheck")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Error Message Quality Tests

        [Fact]
        public void GetDbContext_ErrorMessage_ShouldContainDbContextTypeName()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                BypassUnitOfWorkCheck = false
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
            Assert.Contains("unitOfWorkManager.Begin()", exception.Message);
        }

        [Fact]
        public void GetDbContext_ErrorMessage_ShouldSuggestBypassOption()
        {
            // Arrange
            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                BypassUnitOfWorkCheck = false
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
            Assert.Contains(nameof(MiCakeEFCoreOptions.BypassUnitOfWorkCheck), exception.Message);
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
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddScoped(typeof(IEFCoreContextFactory<TestFactoryDbContext>), typeof(EFCoreContextFactory<TestFactoryDbContext>));

            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestFactoryDbContext))
            {
                BypassUnitOfWorkCheck = true
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
