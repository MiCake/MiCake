using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for ImmediateTransactionInitializer.
    /// Tests immediate transaction initialization for registered DbContext types.
    /// </summary>
    public class ImmediateTransactionInitializerTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IDbContextTypeRegistry> _mockTypeRegistry;
        private readonly Mock<ILogger<ImmediateTransactionInitializer>> _mockLogger;

        public ImmediateTransactionInitializerTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockTypeRegistry = new Mock<IDbContextTypeRegistry>();
            _mockLogger = new Mock<ILogger<ImmediateTransactionInitializer>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockTypeRegistry.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(initializer);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(
                    null!,
                    _mockTypeRegistry.Object,
                    _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullTypeRegistry_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(
                    _mockServiceProvider.Object,
                    null!,
                    _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(
                    _mockServiceProvider.Object,
                    _mockTypeRegistry.Object,
                    null!));
        }

        #endregion

        #region InitializeTransactionsAsync Tests

        [Fact]
        public async Task InitializeTransactionsAsync_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Arrange
            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockTypeRegistry.Object,
                _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                initializer.InitializeTransactionsAsync(null!));
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithNoRegisteredTypes_ShouldLogWarning()
        {
            // Arrange
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes())
                .Returns(Array.Empty<Type>());

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockTypeRegistry.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No DbContext types registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithRegisteredTypes_ShouldCallFactoryForEachType()
        {
            // Arrange
            var dbContextType = typeof(TestInitializerDbContext);
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes())
                .Returns(new[] { dbContextType });

            var mockFactory = new Mock<IEFCoreContextFactory<TestInitializerDbContext>>();
            var mockWrapper = new Mock<EFCoreDbContextWrapper>(MockBehavior.Loose,
                CreateTestDbContext(),
                Mock.Of<ILogger<EFCoreDbContextWrapper>>(),
                new MiCakeEFCoreOptions(dbContextType),
                false);

            mockFactory.Setup(f => f.GetDbContextWrapper())
                .Returns(mockWrapper.Object);

            var factoryType = typeof(IEFCoreContextFactory<>).MakeGenericType(dbContextType);
            _mockServiceProvider.Setup(sp => sp.GetService(factoryType))
                .Returns(mockFactory.Object);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockTypeRegistry.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            mockFactory.Verify(f => f.GetDbContextWrapper(), Times.Once);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WhenFactoryNotRegistered_ShouldLogWarningAndContinue()
        {
            // Arrange
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes())
                .Returns(new[] { typeof(TestInitializerDbContext) });

            var factoryType = typeof(IEFCoreContextFactory<>).MakeGenericType(typeof(TestInitializerDbContext));
            _mockServiceProvider.Setup(sp => sp.GetService(factoryType))
                .Returns(null);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockTypeRegistry.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No factory registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            _mockTypeRegistry.Setup(r => r.GetRegisteredTypes())
                .Returns(Array.Empty<Type>());

            var mockUow = new Mock<IUnitOfWork>();

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockTypeRegistry.Object,
                _mockLogger.Object);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act - Should complete without throwing (no actual async work when no types)
            await initializer.InitializeTransactionsAsync(mockUow.Object, cts.Token);

            // Assert - No exception thrown
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Integration_InitializeTransactionsAsync_WithRealRegistry()
        {
            // Arrange
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestInitializerDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddLogging();

            // Register real components
            var registry = new DbContextTypeRegistry();
            registry.RegisterDbContextType(typeof(TestInitializerDbContext));
            services.AddSingleton<IDbContextTypeRegistry>(registry);

            var efCoreOptions = new MiCakeEFCoreOptions(typeof(TestInitializerDbContext))
            {
                BypassUnitOfWorkCheck = true
            };
            services.AddSingleton<Core.DependencyInjection.IObjectAccessor<MiCakeEFCoreOptions>>(efCoreOptions);

            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddScoped(typeof(IEFCoreContextFactory<TestInitializerDbContext>), typeof(EFCoreContextFactory<TestInitializerDbContext>));
            services.AddScoped<IImmediateTransactionInitializer, ImmediateTransactionInitializer>();

            var provider = services.BuildServiceProvider();
            var initializer = provider.GetRequiredService<IImmediateTransactionInitializer>();
            var uowManager = provider.GetRequiredService<IUnitOfWorkManager>();

            // Act
            using var uow = await uowManager.BeginAsync();
            await initializer.InitializeTransactionsAsync(uow);

            // Assert - No exception thrown
        }

        #endregion

        #region Helper Methods

        private TestInitializerDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<TestInitializerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestInitializerDbContext(options);
        }

        #endregion

        #region Helper Classes

        public class TestInitializerDbContext : DbContext
        {
            public TestInitializerDbContext(DbContextOptions<TestInitializerDbContext> options) : base(options) { }

            public DbSet<TestInitializerEntity> TestEntities { get; set; } = null!;
        }

        public class TestInitializerEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        #endregion
    }
}
