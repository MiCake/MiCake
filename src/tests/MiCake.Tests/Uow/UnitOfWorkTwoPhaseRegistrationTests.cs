using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for two-phase registration pattern in Unit of Work
    /// Tests cover Prepare (Phase 1) and Activate (Phase 2) resource registration
    /// </summary>
    public class UnitOfWorkTwoPhaseRegistrationTests
    {
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWorkTwoPhaseRegistrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
        }

        #region Phase 1 - Prepare Tests

        [Fact]
        public void RegisterResource_ShouldCallPrepareForTransaction()
        {
            // Arrange
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("resource1");
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()));

            // Act
            uow.RegisterResource(mockResource.Object);

            // Assert
            mockResource.Verify(
                r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()), 
                Times.Once,
                "Phase 1: PrepareForTransaction should be called synchronously during registration");
        }

        [Fact]
        public void RegisterResource_ShouldPassOptions()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("resource1");
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()));

            // Act
            uow.RegisterResource(mockResource.Object);

            // Assert
            mockResource.Verify(
                r => r.PrepareForTransaction(options), 
                Times.Once,
                "UnitOfWorkOptions should be passed to PrepareForTransaction");
        }

        [Fact]
        public void RegisterResource_ShouldNotActivateTransaction()
        {
            // Arrange
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("resource1");
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()));
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            uow.RegisterResource(mockResource.Object);

            // Assert
            mockResource.Verify(
                r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), 
                Times.Never,
                "Phase 1 should not call ActivateTransactionAsync");
        }

        #endregion

        #region Phase 2 - Activate Tests (Lazy Mode)

        [Fact]
        public async Task ActivatePendingResourcesAsync_ShouldActivateAllRegisteredResources()
        {
            // Arrange
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var mockResource1 = CreateMockResource("resource1");
            var mockResource2 = CreateMockResource("resource2");
            
            uow.RegisterResource(mockResource1.Object);
            uow.RegisterResource(mockResource2.Object);

            // Act
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Assert
            mockResource1.Verify(
                r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), 
                Times.Once,
                "Phase 2: All registered resources should be activated");
            mockResource2.Verify(
                r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task ActivatePendingResourcesAsync_CalledTwice_ShouldOnlyActivateOnce()
        {
            // Arrange
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var mockResource = CreateMockResource("resource1");
            
            uow.RegisterResource(mockResource.Object);

            // Act
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Assert
            mockResource.Verify(
                r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), 
                Times.Once,
                "Activation should be idempotent");
        }

        [Fact]
        public async Task ActivatePendingResourcesAsync_WithAlreadyInitializedResource_ShouldSkipActivation()
        {
            // Arrange
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("resource1");
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()));
            mockResource.Setup(r => r.IsInitialized).Returns(true);  // Already initialized
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            uow.RegisterResource(mockResource.Object);

            // Act
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Assert
            mockResource.Verify(
                r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), 
                Times.Never,
                "Already initialized resources should not be activated again");
        }

        #endregion

        #region Lazy Mode - Activation on Commit

        [Fact]
        public async Task CommitAsync_InLazyMode_ShouldActivateResourcesFirst()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource("resource1");
            mockResource.Setup(r => r.HasActiveTransaction).Returns(true);
            
            uow.RegisterResource(mockResource.Object);

            var activateCallOrder = 0;
            var commitCallOrder = 0;

            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => activateCallOrder = 1)
                .Returns(Task.CompletedTask);
            
            mockResource.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>()))
                .Callback(() => commitCallOrder = 2)
                .Returns(Task.CompletedTask);

            // Act
            await uow.CommitAsync();

            // Assert
            Assert.Equal(1, activateCallOrder);
            Assert.Equal(2, commitCallOrder);
            Assert.True(activateCallOrder < commitCallOrder, 
                "ActivateTransactionAsync should be called before CommitAsync");
        }

        [Fact]
        public async Task CommitAsync_WithReadOnlyOption_ShouldNotActivateTransactions()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy,
                IsReadOnly = true
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource("resource1");
            
            uow.RegisterResource(mockResource.Object);

            // Act
            await uow.CommitAsync();

            // Assert
            mockResource.Verify(
                r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), 
                Times.Never,
                "Read-only UoW should not activate transactions");
        }

        #endregion

        #region Savepoint Activation Tests

        [Fact]
        public async Task CreateSavepointAsync_InLazyMode_ShouldActivateTransactionsFirst()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource("resource1");
            
            uow.RegisterResource(mockResource.Object);

            // Act
            var savepointName = await uow.CreateSavepointAsync("test_sp");

            // Assert
            mockResource.Verify(
                r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), 
                Times.Once,
                "Savepoint creation should activate transactions first");
            mockResource.Verify(
                r => r.CreateSavepointAsync("test_sp", It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task RollbackToSavepointAsync_InLazyMode_ShouldActivateTransactionsFirst()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource("resource1");
            
            uow.RegisterResource(mockResource.Object);
            await uow.CreateSavepointAsync("test_sp");

            // Reset mock to check rollback activation
            mockResource.Invocations.Clear();

            // Act
            await uow.RollbackToSavepointAsync("test_sp");

            // Assert
            mockResource.Verify(
                r => r.RollbackToSavepointAsync("test_sp", It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        #endregion

        #region Error Handling in Two-Phase Registration

        [Fact]
        public void RegisterResource_PrepareFails_ShouldThrowAndNotRegister()
        {
            // Arrange
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("resource1");
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>())) 
                .Throws(new InvalidOperationException("Prepare failed"));            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => uow.RegisterResource(mockResource.Object));
        }

        [Fact]
        public async Task ActivatePendingResourcesAsync_OneResourceFails_ShouldThrowAggregateException()
        {
            // Arrange
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var mockResource1 = CreateMockResource("resource1");
            var mockResource2 = new Mock<IUnitOfWorkResource>();
            
            mockResource2.Setup(r => r.ResourceIdentifier).Returns("resource2");
            mockResource2.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()));
            mockResource2.Setup(r => r.IsInitialized).Returns(false);
            mockResource2.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Activation failed"));
            
            uow.RegisterResource(mockResource1.Object);
            uow.RegisterResource(mockResource2.Object);

            // Act & Assert
            await Assert.ThrowsAsync<AggregateException>(
                () => ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync());
        }

        #endregion

        #region Nested UoW Two-Phase Registration

        [Fact]
        public void NestedUoW_RegisterResource_ShouldPrepareWithParent()
        {
            // Arrange
            var parent = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_logger, UnitOfWorkOptions.Default, parent);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("resource1");
            mockResource.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()));

            // Act
            child.RegisterResource(mockResource.Object);

            // Assert
            mockResource.Verify(
                r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()), 
                Times.Once,
                "Child UoW should prepare resource through parent");
        }

        [Fact]
        public async Task NestedUoW_ActivatePendingResources_ShouldDelegateToParent()
        {
            // Arrange
            var parent = new UnitOfWork(_logger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_logger, UnitOfWorkOptions.Default, parent);
            var mockResource = CreateMockResource("resource1");
            
            child.RegisterResource(mockResource.Object);

            // Act
            await ((IUnitOfWorkInternal)child).ActivatePendingResourcesAsync();

            // Assert
            // Child delegates to parent, so activation should happen
            mockResource.Verify(
                r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), 
                Times.Once,
                "Nested UoW should delegate activation to parent");
        }

        #endregion

        #region Helper Methods

        private static Mock<IUnitOfWorkResource> CreateMockResource(string identifier)
        {
            var mock = new Mock<IUnitOfWorkResource>();
            
            mock.Setup(r => r.ResourceIdentifier).Returns(identifier);
            mock.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()));
            mock.Setup(r => r.IsInitialized).Returns(false);
            mock.Setup(r => r.HasActiveTransaction).Returns(false);
            mock.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => mock.Setup(r => r.IsInitialized).Returns(true))
                .Returns(Task.CompletedTask);
            mock.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.Setup(r => r.CreateSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((name, ct) => Task.FromResult(name));
            mock.Setup(r => r.RollbackToSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mock.Setup(r => r.ReleaseSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            return mock;
        }

        #endregion
    }
}
