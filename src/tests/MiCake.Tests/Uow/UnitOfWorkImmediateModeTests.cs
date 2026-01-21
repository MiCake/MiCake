using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests specifically for UnitOfWork Immediate initialization mode
    /// Tests that resources are activated immediately when UoW is created
    /// </summary>
    public class UnitOfWorkImmediateModeTests
    {
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWorkImmediateModeTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
        }

        #region Immediate Mode Activation Tests

        [Fact]
        public async Task ActivatePendingResources_InImmediateMode_ShouldActivateResourcesImmediately()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource();

            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => mockResource.Setup(r => r.IsInitialized).Returns(true))
                .Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);

            // Act
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Assert
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(uow.HasActiveTransactions);
        }

        [Fact]
        public async Task ActivatePendingResources_WithMultipleResources_ShouldActivateAll()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var uow = new UnitOfWork(_logger, options, null);
            
            var mockResource1 = CreateMockResource();
            var mockResource2 = CreateMockResource();
            var mockResource3 = CreateMockResource();

            foreach (var resource in new[] { mockResource1, mockResource2, mockResource3 })
            {
                resource.Setup(r => r.IsInitialized).Returns(false);
                resource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                    .Callback(() => resource.Setup(r => r.IsInitialized).Returns(true))
                    .Returns(Task.CompletedTask);
            }

            uow.RegisterResource(mockResource1.Object);
            uow.RegisterResource(mockResource2.Object);
            uow.RegisterResource(mockResource3.Object);

            // Act
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Assert
            mockResource1.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource2.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource3.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ActivatePendingResources_AlreadyInitializedResource_ShouldSkipActivation()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource();

            mockResource.Setup(r => r.IsInitialized).Returns(true);
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);

            // Act
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Assert
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ActivatePendingResources_CalledMultipleTimes_ShouldOnlyActivateOnce()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource();

            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => mockResource.Setup(r => r.IsInitialized).Returns(true))
                .Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);

            // Act
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Assert
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Immediate Mode with Nested UoW Tests

        [Fact]
        public async Task NestedUoW_InImmediateMode_ShouldDelegateActivationToParent()
        {
            // Arrange
            var parentOptions = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var parent = new UnitOfWork(_logger, parentOptions, null);
            var child = new UnitOfWork(_logger, parentOptions, parent);

            var mockResource = CreateMockResource();
            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => mockResource.Setup(r => r.IsInitialized).Returns(true))
                .Returns(Task.CompletedTask);

            // Register resource through child (should register to parent)
            child.RegisterResource(mockResource.Object);

            // Act - Activate through child should activate parent's resources
            await ((IUnitOfWorkInternal)child).ActivatePendingResourcesAsync();

            // Assert
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(parent.HasActiveTransactions);
        }

        #endregion

        #region Immediate Mode Commit Tests

        [Fact]
        public async Task CommitAsync_InImmediateMode_ShouldNotActivateAgain()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource();

            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => mockResource.Setup(r => r.IsInitialized).Returns(true))
                .Returns(Task.CompletedTask);
            mockResource.Setup(r => r.HasActiveTransaction).Returns(true);
            mockResource.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockResource.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);

            // Pre-activate (simulating immediate mode activation at creation)
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Act
            await uow.CommitAsync();

            // Assert - Should not activate again
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Read-Only Mode Tests

        [Fact]
        public async Task ActivatePendingResources_InReadOnlyMode_ShouldNotActivate()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                IsReadOnly = true
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource();

            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            uow.RegisterResource(mockResource.Object);

            // Act
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Assert
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            Assert.False(uow.HasActiveTransactions);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ActivatePendingResources_WithResourceThrowingException_ShouldPropagateException()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource();

            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Activation failed"));

            uow.RegisterResource(mockResource.Object);

            // Act & Assert
            await Assert.ThrowsAsync<AggregateException>(async () =>
                await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync());
        }

        [Fact]
        public async Task ActivatePendingResources_WithMultipleResourcesFailing_ShouldAggregateExceptions()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var uow = new UnitOfWork(_logger, options, null);
            
            var mockResource1 = CreateMockResource();
            var mockResource2 = CreateMockResource();

            mockResource1.Setup(r => r.IsInitialized).Returns(false);
            mockResource1.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Resource 1 failed"));

            mockResource2.Setup(r => r.IsInitialized).Returns(false);
            mockResource2.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Resource 2 failed"));

            uow.RegisterResource(mockResource1.Object);
            uow.RegisterResource(mockResource2.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
                await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync());

            Assert.Equal(2, exception.InnerExceptions.Count);
        }

        #endregion

        #region Savepoint Tests with Immediate Mode

        [Fact]
        public async Task CreateSavepointAsync_InImmediateMode_ShouldWorkWithAlreadyActivatedTransaction()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            var uow = new UnitOfWork(_logger, options, null);
            var mockResource = CreateMockResource();

            mockResource.Setup(r => r.IsInitialized).Returns(false);
            mockResource.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => mockResource.Setup(r => r.IsInitialized).Returns(true))
                .Returns(Task.CompletedTask);
            mockResource.Setup(r => r.CreateSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((name, ct) => Task.FromResult(name));

            uow.RegisterResource(mockResource.Object);

            // Pre-activate
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            // Act
            var savepointName = await uow.CreateSavepointAsync("test_sp");

            // Assert
            Assert.Equal("test_sp", savepointName);
            mockResource.Verify(r => r.CreateSavepointAsync("test_sp", It.IsAny<CancellationToken>()), Times.Once);
            // Should not activate again
            mockResource.Verify(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Helper Methods

        private static Mock<IUnitOfWorkResource> CreateMockResource()
        {
            var mock = new Mock<IUnitOfWorkResource>();
            var uniqueId = Guid.NewGuid().ToString();
            
            mock.Setup(r => r.ResourceIdentifier).Returns(uniqueId);
            mock.Setup(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()));
            mock.Setup(r => r.IsInitialized).Returns(true);
            mock.Setup(r => r.HasActiveTransaction).Returns(false);
            mock.Setup(r => r.ActivateTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.Setup(r => r.CreateSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((name, ct) => Task.FromResult(name ?? "sp_default"));
            mock.Setup(r => r.RollbackToSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mock.Setup(r => r.ReleaseSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return mock;
        }

        #endregion
    }
}
