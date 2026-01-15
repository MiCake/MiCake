using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for ImmediateTransactionLifetimeHook.
    /// Tests lifecycle hook behavior for immediate transaction initialization.
    /// </summary>
    public class ImmediateTransactionLifetimeHookTests
    {
        private readonly Mock<IImmediateTransactionInitializer> _mockInitializer;
        private readonly Mock<ILogger<ImmediateTransactionLifetimeHook>> _mockLogger;

        public ImmediateTransactionLifetimeHookTests()
        {
            _mockInitializer = new Mock<IImmediateTransactionInitializer>();
            _mockLogger = new Mock<ILogger<ImmediateTransactionLifetimeHook>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var hook = new ImmediateTransactionLifetimeHook(
                _mockInitializer.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(hook);
        }

        [Fact]
        public void Constructor_WithNullInitializer_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionLifetimeHook(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionLifetimeHook(_mockInitializer.Object, null!));
        }

        #endregion

        #region ApplicableMode Tests

        [Fact]
        public void ApplicableMode_ShouldBeImmediate()
        {
            // Arrange
            var hook = new ImmediateTransactionLifetimeHook(
                _mockInitializer.Object,
                _mockLogger.Object);

            // Act
            var mode = hook.ApplicableMode;

            // Assert
            Assert.Equal(TransactionInitializationMode.Immediate, mode);
        }

        #endregion

        #region OnUnitOfWorkCreatedAsync Tests

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_ShouldCallInitializer()
        {
            // Arrange
            var hook = new ImmediateTransactionLifetimeHook(
                _mockInitializer.Object,
                _mockLogger.Object);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var options = UnitOfWorkOptions.Immediate;

            // Act
            await hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options);

            // Assert
            _mockInitializer.Verify(
                i => i.InitializeTransactionsAsync(mockUow.Object, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_ShouldLogDebugMessage()
        {
            // Arrange
            var hook = new ImmediateTransactionLifetimeHook(
                _mockInitializer.Object,
                _mockLogger.Object);

            var mockUow = new Mock<IUnitOfWork>();
            var uowId = Guid.NewGuid();
            mockUow.Setup(u => u.Id).Returns(uowId);
            var options = UnitOfWorkOptions.Immediate;

            // Act
            await hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Immediate transaction initialization")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_ShouldPassCancellationToken()
        {
            // Arrange
            var hook = new ImmediateTransactionLifetimeHook(
                _mockInitializer.Object,
                _mockLogger.Object);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var options = UnitOfWorkOptions.Immediate;
            using var cts = new CancellationTokenSource();

            // Act
            await hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options, cts.Token);

            // Assert
            _mockInitializer.Verify(
                i => i.InitializeTransactionsAsync(mockUow.Object, cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_WhenInitializerThrows_ShouldPropagate()
        {
            // Arrange
            _mockInitializer.Setup(i => i.InitializeTransactionsAsync(It.IsAny<IUnitOfWork>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Initializer failed"));

            var hook = new ImmediateTransactionLifetimeHook(
                _mockInitializer.Object,
                _mockLogger.Object);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var options = UnitOfWorkOptions.Immediate;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options));
        }

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_WithCancelledToken_ShouldThrowOperationCancelledException()
        {
            // Arrange
            _mockInitializer.Setup(i => i.InitializeTransactionsAsync(It.IsAny<IUnitOfWork>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var hook = new ImmediateTransactionLifetimeHook(
                _mockInitializer.Object,
                _mockLogger.Object);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var options = UnitOfWorkOptions.Immediate;
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options, cts.Token));
        }

        #endregion

        #region Integration Tests with Different UoW Options

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_WithLazyOptions_ShouldStillCallInitializer()
        {
            // Note: The hook doesn't check the options, that's the UoW manager's responsibility
            // The hook is only invoked when ApplicableMode matches
            
            // Arrange
            var hook = new ImmediateTransactionLifetimeHook(
                _mockInitializer.Object,
                _mockLogger.Object);

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var options = UnitOfWorkOptions.Default; // Lazy mode

            // Act
            await hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options);

            // Assert - The hook is called regardless; filtering is done by UoW manager
            _mockInitializer.Verify(
                i => i.InitializeTransactionsAsync(mockUow.Object, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion
    }
}
