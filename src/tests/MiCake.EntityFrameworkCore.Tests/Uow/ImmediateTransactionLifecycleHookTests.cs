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
    /// Unit tests for ImmediateTransactionLifecycleHook
    /// Tests the lifecycle hook that handles immediate transaction initialization
    /// </summary>
    public class ImmediateTransactionLifecycleHookTests
    {
        private readonly Mock<IImmediateTransactionInitializer> _mockInitializer;
        private readonly Mock<ILogger<ImmediateTransactionLifecycleHook>> _mockLogger;
        private readonly ImmediateTransactionLifecycleHook _hook;

        public ImmediateTransactionLifecycleHookTests()
        {
            _mockInitializer = new Mock<IImmediateTransactionInitializer>();
            _mockLogger = new Mock<ILogger<ImmediateTransactionLifecycleHook>>();
            _hook = new ImmediateTransactionLifecycleHook(_mockInitializer.Object, _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullInitializer_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionLifecycleHook(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ImmediateTransactionLifecycleHook(_mockInitializer.Object, null));
        }

        #endregion

        #region ApplicableMode Tests

        [Fact]
        public void ApplicableMode_ShouldReturnImmediate()
        {
            // Act
            var mode = _hook.ApplicableMode;

            // Assert
            Assert.Equal(TransactionInitializationMode.Immediate, mode);
        }

        #endregion

        #region OnUnitOfWorkCreatedAsync Tests

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_WithValidParameters_ShouldCallInitializer()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var options = new UnitOfWorkOptions();
            var cancellationToken = CancellationToken.None;

            // Act
            await _hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options, cancellationToken);

            // Assert
            _mockInitializer.Verify(i => i.InitializeTransactionsAsync(mockUow.Object, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Arrange
            var options = new UnitOfWorkOptions();
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _hook.OnUnitOfWorkCreatedAsync(null, options, cancellationToken));
        }

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _hook.OnUnitOfWorkCreatedAsync(mockUow.Object, null, cancellationToken));
        }

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_WhenInitializerThrows_ShouldPropagateException()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var options = new UnitOfWorkOptions();
            var expectedException = new InvalidOperationException("Initialization failed");

            _mockInitializer.Setup(i => i.InitializeTransactionsAsync(It.IsAny<IUnitOfWork>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options));

            Assert.Equal("Initialization failed", exception.Message);
        }

        [Fact]
        public async Task OnUnitOfWorkCreatedAsync_WithDifferentCancellationTokens_ShouldPassCorrectToken()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());
            var options = new UnitOfWorkOptions();
            var cancellationToken = new CancellationToken(true);

            // Act
            await _hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options, cancellationToken);

            // Assert
            _mockInitializer.Verify(i => i.InitializeTransactionsAsync(mockUow.Object, cancellationToken), Times.Once);
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public async Task IntegrationScenario_UowCreationWithImmediateMode_ShouldInitializeTransactions()
        {
            // Arrange: Simulate a real UoW creation scenario
            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Id).Returns(Guid.NewGuid());

            var options = new UnitOfWorkOptions
            {
                TransactionInitializationMode = TransactionInitializationMode.Immediate
            };

            // Act: Hook is called during UoW creation
            await _hook.OnUnitOfWorkCreatedAsync(mockUow.Object, options);

            // Assert: Initializer should have been called to set up transactions
            _mockInitializer.Verify(i => i.InitializeTransactionsAsync(mockUow.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IntegrationScenario_MultipleUowCreations_ShouldInitializeEachSeparately()
        {
            // Arrange: Multiple UoW instances
            var uow1 = new Mock<IUnitOfWork>();
            uow1.Setup(u => u.Id).Returns(Guid.NewGuid());

            var uow2 = new Mock<IUnitOfWork>();
            uow2.Setup(u => u.Id).Returns(Guid.NewGuid());

            var options = new UnitOfWorkOptions();

            // Act: Create multiple UoWs
            await _hook.OnUnitOfWorkCreatedAsync(uow1.Object, options);
            await _hook.OnUnitOfWorkCreatedAsync(uow2.Object, options);

            // Assert: Each UoW should trigger initialization
            _mockInitializer.Verify(i => i.InitializeTransactionsAsync(uow1.Object, It.IsAny<CancellationToken>()), Times.Once);
            _mockInitializer.Verify(i => i.InitializeTransactionsAsync(uow2.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}