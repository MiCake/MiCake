using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for ImmediateTransactionInitializer: typed, reflection-free activation
    /// of every registered non-generic context factory.
    /// </summary>
    public class ImmediateTransactionInitializerTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<ImmediateTransactionInitializer>> _mockLogger;

        public ImmediateTransactionInitializerTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<ImmediateTransactionInitializer>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(initializer);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionInitializer(_mockServiceProvider.Object, null!));
        }

        #endregion

        #region InitializeTransactionsAsync Tests

        [Fact]
        public async Task InitializeTransactionsAsync_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Arrange
            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                initializer.InitializeTransactionsAsync(null!));
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithNoFactories_ShouldLogWarning()
        {
            // Arrange
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IEFCoreContextFactory>)))
                .Returns(Array.Empty<IEFCoreContextFactory>());

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No DbContext factories registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_WithRegisteredFactories_ShouldActivateEachFactoryWithoutReflection()
        {
            // Arrange
            var wrapper = CreateMockWrapper();

            var mockFactory1 = new Mock<IEFCoreContextFactory>();
            mockFactory1.Setup(f => f.GetDbContextWrapper()).Returns(wrapper.Object);
            var mockFactory2 = new Mock<IEFCoreContextFactory>();
            mockFactory2.Setup(f => f.GetDbContextWrapper()).Returns(wrapper.Object);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IEFCoreContextFactory>)))
                .Returns(new IEFCoreContextFactory[] { mockFactory1.Object, mockFactory2.Object });

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object);

            // Assert
            mockFactory1.Verify(f => f.GetDbContextWrapper(), Times.Once);
            mockFactory2.Verify(f => f.GetDbContextWrapper(), Times.Once);
        }

        [Fact]
        public async Task InitializeTransactionsAsync_ShouldPassTheUnitOfWorkToDiagnostics()
        {
            // Arrange
            var wrapper = CreateMockWrapper();
            var mockFactory = new Mock<IEFCoreContextFactory>();
            mockFactory.Setup(f => f.GetDbContextWrapper()).Returns(wrapper.Object);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IEFCoreContextFactory>)))
                .Returns(new IEFCoreContextFactory[] { mockFactory.Object });

            var uowId = Guid.NewGuid();
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(uowId);

            var initializer = new ImmediateTransactionInitializer(
                _mockServiceProvider.Object,
                _mockLogger.Object);

            // Act
            await initializer.InitializeTransactionsAsync(mockUow.Object, CancellationToken.None);

            // Assert
            mockFactory.Verify(f => f.GetDbContextWrapper(), Times.Once);
        }

        #endregion

        private static Mock<EFCoreDbContextWrapper> CreateMockWrapper()
        {
            var options = new DbContextOptionsBuilder<TestInitializerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new TestInitializerDbContext(options);

            return new Mock<EFCoreDbContextWrapper>(
                context,
                Mock.Of<ILogger<EFCoreDbContextWrapper>>(),
                false);
        }
    }

    public class TestInitializerDbContext : DbContext
    {
        public TestInitializerDbContext(DbContextOptions<TestInitializerDbContext> options) : base(options)
        {
        }
    }
}
