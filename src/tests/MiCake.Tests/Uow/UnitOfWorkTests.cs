using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for UnitOfWork core functionality
    /// Tests cover two-phase registration, commit/rollback, savepoints, events, and lifecycle
    /// </summary>
    public class UnitOfWorkTests
    {
        private readonly Mock<ILogger<UnitOfWork>> _mockLogger;
        private readonly UnitOfWorkOptions _defaultOptions;

        public UnitOfWorkTests()
        {
            _mockLogger = new Mock<ILogger<UnitOfWork>>();
            _defaultOptions = new UnitOfWorkOptions();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateUnitOfWork()
        {
            // Act
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);

            // Assert
            Assert.NotNull(uow);
            Assert.NotEqual(Guid.Empty, uow.Id);
            Assert.False(uow.IsCompleted);
            Assert.Null(uow.Parent);
        }

        [Fact]
        public void Constructor_WithParent_ShouldSetParentReference()
        {
            // Arrange
            var parent = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);

            // Act
            var child = new UnitOfWork(_defaultOptions, parent, _mockLogger.Object);

            // Assert
            Assert.Same(parent, child.Parent);
        }

        #endregion

        #region Two-Phase Registration Tests

        [Fact]
        public void RegisterResource_ShouldCallPrepareForTransaction()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));

            // Act
            uow.RegisterResource(mockResource.Object);

            // Assert
            mockResource.Verify(r => r.PrepareForTransaction(_defaultOptions.IsolationLevel), Times.Once);
        }

        [Fact]
        public void RegisterResource_WithNestedUow_ShouldRegisterToParent()
        {
            // Arrange
            var parent = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var child = new UnitOfWork(_defaultOptions, parent, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));

            // Act
            child.RegisterResource(mockResource.Object);

            // Assert
            // Resource should be registered to parent
            mockResource.Verify(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()), Times.Once);
        }

        [Fact]
        public void RegisterResource_SameResourceTwice_ShouldOnlyRegisterOnce()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));

            // Act
            uow.RegisterResource(mockResource.Object);
            uow.RegisterResource(mockResource.Object);

            // Assert
            mockResource.Verify(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()), Times.Once);
        }

        #endregion

        #region CommitAsync Tests - Lazy Mode

        [Fact]
        public async Task CommitAsync_InLazyMode_ShouldActivateResourcesBeforeCommit()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            var uow = new UnitOfWork(options, null, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => mockResource.Setup(r => r.IsInitialized).Returns(true))
                .Returns(Task.CompletedTask);
            mockResource.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);

            // Act
            await uow.CommitAsync();

            // Assert
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CommitAsync_WithNoResources_ShouldCompleteSuccessfully()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);

            // Act
            await uow.CommitAsync();

            // Assert
            Assert.True(uow.IsCompleted);
        }

        [Fact]
        public async Task CommitAsync_InReadOnlyMode_ShouldNotActivateTransactions()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                IsReadOnly = true
            };
            var uow = new UnitOfWork(options, null, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);

            // Act
            await uow.CommitAsync();

            // Assert
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CommitAsync_AlreadyCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            await uow.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
        }

        #endregion

        #region RollbackAsync Tests

        [Fact]
        public async Task RollbackAsync_WithResources_ShouldRollbackAllResources()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var mockResource1 = new Mock<IUnitOfWorkResource>();
            var mockResource2 = new Mock<IUnitOfWorkResource>();
            mockResource1.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource2.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource1.Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockResource2.Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource1.Object);
            uow.RegisterResource(mockResource2.Object);

            // Act
            await uow.RollbackAsync();

            // Assert
            mockResource1.Verify(r => r.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource2.Verify(r => r.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(uow.IsCompleted);
        }

        [Fact]
        public async Task RollbackAsync_AlreadyCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            await uow.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackAsync());
        }

        #endregion

        #region Savepoint Tests

        [Fact]
        public async Task CreateSavepointAsync_InLazyMode_ShouldActivateTransactionsFirst()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            var uow = new UnitOfWork(options, null, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => mockResource.Setup(r => r.IsInitialized).Returns(true))
                .Returns(Task.CompletedTask);
            mockResource.Setup(r => r.CreateSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);

            // Act
            var savepointName = await uow.CreateSavepointAsync("test_savepoint");

            // Assert
            Assert.NotNull(savepointName);
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource.Verify(r => r.CreateSavepointAsync("test_savepoint", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateSavepointAsync_WithNullName_ShouldGenerateName()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);

            // Act
            var savepointName = await uow.CreateSavepointAsync();

            // Assert
            Assert.NotNull(savepointName);
            Assert.StartsWith("sp_", savepointName);
        }

        [Fact]
        public async Task RollbackToSavepointAsync_ShouldRollbackAllResources()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource.Setup(r => r.IsInitialized).Returns(true);
            mockResource.Setup(r => r.RollbackToSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);
            var savepointName = await uow.CreateSavepointAsync("test_sp");

            // Act
            await uow.RollbackToSavepointAsync("test_sp");

            // Assert
            mockResource.Verify(r => r.RollbackToSavepointAsync("test_sp", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReleaseSavepointAsync_ShouldReleaseFromAllResources()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource.Setup(r => r.IsInitialized).Returns(true);
            mockResource.Setup(r => r.ReleaseSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);
            var savepointName = await uow.CreateSavepointAsync("test_sp");

            // Act
            await uow.ReleaseSavepointAsync("test_sp");

            // Assert
            mockResource.Verify(r => r.ReleaseSavepointAsync("test_sp", It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Event Tests

        [Fact]
        public async Task CommitAsync_ShouldRaiseOnCommittingAndOnCommittedEvents()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var committingRaised = false;
            var committedRaised = false;

            uow.OnCommitting += (sender, args) =>
            {
                committingRaised = true;
                Assert.Equal(uow.Id, args.UnitOfWorkId);
            };

            uow.OnCommitted += (sender, args) =>
            {
                committedRaised = true;
                Assert.Equal(uow.Id, args.UnitOfWorkId);
            };

            // Act
            await uow.CommitAsync();

            // Assert
            Assert.True(committingRaised);
            Assert.True(committedRaised);
        }

        [Fact]
        public async Task RollbackAsync_ShouldRaiseOnRolledBackEvent()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var rolledBackRaised = false;

            uow.OnRolledBack += (sender, args) =>
            {
                rolledBackRaised = true;
                Assert.Equal(uow.Id, args.UnitOfWorkId);
            };

            // Act
            await uow.RollbackAsync();

            // Assert
            Assert.True(rolledBackRaised);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_WithoutCommitOrRollback_ShouldLogWarning()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);

            // Act
            uow.Dispose();

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("disposed without being completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Dispose_AfterCommit_ShouldNotLogWarning()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            await uow.CommitAsync();

            // Act
            uow.Dispose();

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("disposed without being completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        #endregion

        #region MarkAsCompleted Tests

        [Fact]
        public void MarkAsCompleted_ShouldSetIsCompletedToTrue()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);

            // Act
            uow.MarkAsCompleted();

            // Assert
            Assert.True(uow.IsCompleted);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task CommitAsync_WithResourceThrowingException_ShouldPropagateException()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            uow.RegisterResource(mockResource.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
        }

        [Fact]
        public async Task RollbackAsync_WithResourceThrowingException_ShouldContinueRollingBackOthers()
        {
            // Arrange
            var uow = new UnitOfWork(_defaultOptions, null, _mockLogger.Object);
            var mockResource1 = new Mock<IUnitOfWorkResource>();
            var mockResource2 = new Mock<IUnitOfWorkResource>();
            
            mockResource1.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            mockResource2.Setup(r => r.PrepareForTransaction(It.IsAny<IsolationLevel?>()));
            
            mockResource1.Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Rollback failed"));
            mockResource2.Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource1.Object);
            uow.RegisterResource(mockResource2.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackAsync());
            
            // Both should have been attempted
            mockResource1.Verify(r => r.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource2.Verify(r => r.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}
