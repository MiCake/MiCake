using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.DDD.Tests.Uow
{
    /// <summary>
    /// Comprehensive tests for Unit of Work functionality
    /// Tests cover: basic operations, nested transactions, event hooks, and savepoints
    /// </summary>
    public class UnitOfWork_Tests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWorkManager _uowManager;

        public UnitOfWork_Tests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            _serviceProvider = services.BuildServiceProvider();
            _uowManager = _serviceProvider.GetRequiredService<IUnitOfWorkManager>();
        }

        /// <summary>
        /// Helper method to create a UnitOfWork instance directly for testing internal behavior
        /// </summary>
        private UnitOfWork CreateUnitOfWork(UnitOfWorkOptions? options = null)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>();
            return new UnitOfWork(logger, options);
        }

        #region Basic Operations

        [Fact]
        public void UnitOfWork_ShouldBeCreated()
        {
            // Act
            using var uow = _uowManager.Begin();

            // Assert
            Assert.NotNull(uow);
            Assert.NotEqual(Guid.Empty, uow.Id);
            Assert.False(uow.IsDisposed);
            Assert.False(uow.IsCompleted);
            Assert.Equal(uow.Id, _uowManager.Current?.Id);
        }

        [Fact]
        public void UnitOfWork_WithOptions_ShouldHaveConfiguredIsolationLevel()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable
            };

            // Act
            using var uow = _uowManager.Begin(options);

            // Assert
            Assert.Equal(IsolationLevel.Serializable, uow.IsolationLevel);
        }

        [Fact]
        public async Task UnitOfWork_Commit_ShouldMarkAsCompleted()
        {
            // Arrange
            using var uow = _uowManager.Begin();

            // Act
            await uow.CommitAsync();

            // Assert
            Assert.True(uow.IsCompleted);
        }

        [Fact]
        public async Task UnitOfWork_Rollback_CallsRollback()
        {
            // Arrange
            using var uow = _uowManager.Begin();

            // Act
            await uow.RollbackAsync();

            // Assert - Rollback should complete without throwing
            Assert.True(true); // Test passes if no exception
        }

        [Fact]
        public async Task UnitOfWork_CannotCommitTwice()
        {
            // Arrange
            using var uow = _uowManager.Begin();
            await uow.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
        }

        [Fact]
        public async Task UnitOfWork_MarkAsCompleted_SkipsActualCommit()
        {
            // Arrange
            using var uow = _uowManager.Begin();

            // Act
            await uow.MarkAsCompletedAsync();
            await uow.CommitAsync(); // Should not throw

            // Assert
            Assert.True(uow.IsCompleted);
        }

        #endregion

        #region Nested Transactions

        [Fact]
        public void Nested_UnitOfWork_ShouldBeCreated()
        {
            // Arrange & Act
            using var outer = _uowManager.Begin();
            using var inner = _uowManager.Begin();

            // Assert
            Assert.NotNull(inner);
            Assert.NotEqual(outer.Id, inner.Id);
            Assert.NotNull(inner.Parent);
            
            // Note: The actual parent ID relationship is maintained internally by the wrapper
            // Current should exist and be a UoW
            Assert.NotNull(_uowManager.Current);
        }

        [Fact]
        public async Task Nested_UnitOfWork_Commit_OnlyMarksCompleteNotParent()
        {
            // Arrange
            using var outer = _uowManager.Begin();
            using var inner = _uowManager.Begin();

            // Act
            await inner.CommitAsync();

            // Assert
            Assert.True(inner.IsCompleted);
            Assert.False(outer.IsCompleted); // Parent should not be completed yet
        }

        [Fact]
        public async Task Nested_UnitOfWork_Rollback_SignalsParentToRollback()
        {
            // Arrange
            using var outer = _uowManager.Begin();
            using var inner = _uowManager.Begin();

            // Act
            await inner.RollbackAsync();

            // Assert - Parent should be marked for rollback when trying to commit
            await Assert.ThrowsAsync<InvalidOperationException>(() => outer.CommitAsync());
        }

        [Fact]
        public async Task UnitOfWork_RequiresNew_CreatesNewRoot()
        {
            // Arrange
            using var first = _uowManager.Begin();
            
            // Act
            using var second = _uowManager.Begin(requiresNew: true);

            // Assert
            Assert.Null(second.Parent); // Should be a new root
            Assert.NotEqual(first.Id, second.Id);
        }

        #endregion

        #region Event Hooks

        [Fact]
        public async Task OnCommitting_Event_ShouldFire_BeforeCommit()
        {
            // Arrange
            using var uow = _uowManager.Begin();
            bool eventFired = false;
            bool wasCompletedDuringEvent = false;

            uow.OnCommitting += (sender, args) =>
            {
                eventFired = true;
                wasCompletedDuringEvent = uow.IsCompleted;
            };

            // Act
            await uow.CommitAsync();

            // Assert
            Assert.True(eventFired);
            Assert.False(wasCompletedDuringEvent); // Should not be completed during event
        }

        [Fact]
        public async Task OnCommitted_Event_ShouldFire_AfterCommit()
        {
            // Arrange
            using var uow = _uowManager.Begin();
            bool eventFired = false;
            bool wasCompletedDuringEvent = false;

            uow.OnCommitted += (sender, args) =>
            {
                eventFired = true;
                wasCompletedDuringEvent = uow.IsCompleted;
            };

            // Act
            await uow.CommitAsync();

            // Assert
            Assert.True(eventFired);
            Assert.True(wasCompletedDuringEvent); // Should be completed during event
        }

        [Fact]
        public async Task OnRollingBack_And_OnRolledBack_Events_ShouldFire()
        {
            // Arrange
            using var uow = _uowManager.Begin();
            bool rollingBackFired = false;
            bool rolledBackFired = false;

            uow.OnRollingBack += (sender, args) => rollingBackFired = true;
            uow.OnRolledBack += (sender, args) => rolledBackFired = true;

            // Act
            await uow.RollbackAsync();

            // Assert
            Assert.True(rollingBackFired);
            Assert.True(rolledBackFired);
        }

        [Fact]
        public async Task Event_Exception_ShouldNotBreakFlow()
        {
            // Arrange
            using var uow = _uowManager.Begin();
            bool onCommittedFired = false;

            uow.OnCommitting += (sender, args) =>
            {
                throw new Exception("Simulated error");
            };

            uow.OnCommitted += (sender, args) =>
            {
                onCommittedFired = true;
            };

            // Act
            await uow.CommitAsync(); // Should not throw

            // Assert
            Assert.True(uow.IsCompleted);
            Assert.True(onCommittedFired); // Next event should still fire
        }

        #endregion

        #region Savepoints

        [Fact]
        public async Task Savepoint_RequiresNonEmptyName()
        {
            // Arrange
            using var uow = _uowManager.Begin();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => uow.CreateSavepointAsync(""));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => uow.CreateSavepointAsync(null!));
        }

        [Fact]
        public async Task Savepoint_CannotBeCreated_AfterComplete()
        {
            // Arrange
            using var uow = _uowManager.Begin();
            await uow.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => uow.CreateSavepointAsync("test"));
        }

        // Note: Testing savepoints with actual resources requires EF Core integration
        // These tests verify the interface and basic validation

        [Fact]
        public async Task Savepoint_CanBeCreated_AfterRegisteringResource()
        {
            // Arrange
            var logger = _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>();
            using var uow = new UnitOfWork(logger);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("test-resource");
            mockResource.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockResource.Setup(r => r.CreateSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("test-savepoint");

            // Act
            uow.RegisterResource(mockResource.Object);
            var savepointName = await uow.CreateSavepointAsync("test-savepoint");

            // Assert
            Assert.Equal("test-savepoint", savepointName);
            mockResource.Verify(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Once);
            mockResource.Verify(r => r.CreateSavepointAsync("test-savepoint", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Savepoint_CanBeRolledBack_AfterRegisteringResource()
        {
            // Arrange
            var logger = _serviceProvider.GetRequiredService<ILogger<UnitOfWork>>();
            using var uow = new UnitOfWork(logger);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("test-resource");
            mockResource.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockResource.Setup(r => r.RollbackToSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            uow.RegisterResource(mockResource.Object);
            await uow.CreateSavepointAsync("test-savepoint");
            await uow.RollbackToSavepointAsync("test-savepoint");

            // Assert
            mockResource.Verify(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Once);
            mockResource.Verify(r => r.RollbackToSavepointAsync("test-savepoint", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Savepoint_CannotBeCreated_WithoutResourceRegistration()
        {
            // Arrange
            using var uow = CreateUnitOfWork();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => uow.CreateSavepointAsync("test-savepoint"));
        }

        [Fact]
        public async Task Savepoint_CannotBeRolledBack_WithoutResourceRegistration()
        {
            // Arrange
            using var uow = CreateUnitOfWork();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => uow.RollbackToSavepointAsync("test-savepoint"));
        }

        #endregion

        #region Lazy Transaction Initialization

        [Fact]
        public async Task LazyInitialization_TransactionsStarted_OnCommit_WithResource()
        {
            // Arrange
            using var uow = CreateUnitOfWork();
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("test-resource");
            mockResource.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockResource.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockResource.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            uow.RegisterResource(mockResource.Object);
            await uow.CommitAsync();

            // Assert
            Assert.True(uow.HasActiveTransactions);
            mockResource.Verify(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Once);
            mockResource.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LazyInitialization_TransactionsNotStarted_OnCommit_ReadOnly()
        {
            // Arrange
            using var uow = CreateUnitOfWork(UnitOfWorkOptions.ReadOnly);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("test-resource");

            // Act
            uow.RegisterResource(mockResource.Object);
            await uow.CommitAsync();

            // Assert
            Assert.False(uow.HasActiveTransactions);
            mockResource.Verify(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LazyInitialization_TransactionsNotStarted_OnCommit_WithoutResources()
        {
            // Arrange
            using var uow = CreateUnitOfWork();

            // Act
            await uow.CommitAsync();

            // Assert
            Assert.False(uow.HasActiveTransactions);
        }

        #endregion

        #region Transaction Initialization Modes

        [Fact]
        public async Task ImmediateInitializationMode_ShouldStartTransactions_OnResourceRegistration()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Immediate
            };
            using var uow = CreateUnitOfWork(options);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("test-resource");
            mockResource.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            uow.RegisterResource(mockResource.Object);

            // Assert - Transactions should be started immediately
            Assert.True(uow.HasActiveTransactions);
            mockResource.Verify(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LazyInitializationMode_ShouldNotStartTransactions_OnResourceRegistration()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                InitializationMode = TransactionInitializationMode.Lazy
            };
            using var uow = CreateUnitOfWork(options);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("test-resource");
            mockResource.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            uow.RegisterResource(mockResource.Object);

            // Assert - Transactions should NOT be started immediately
            Assert.False(uow.HasActiveTransactions);
            mockResource.Verify(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Isolation Level Consistency

        [Fact]
        public async Task IsolationLevel_AppliedCorrectly_OnTransactionStart()
        {
            // Arrange
            var options = new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable
            };
            using var uow = CreateUnitOfWork(options);
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("test-resource");
            mockResource.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            uow.RegisterResource(mockResource.Object);
            await uow.CommitAsync(); // This should start transactions

            // Assert
            mockResource.Verify(r => r.BeginTransactionAsync(IsolationLevel.Serializable, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IsolationLevel_DefaultApplied_WhenNotSpecified()
        {
            // Arrange
            using var uow = CreateUnitOfWork(); // Default options
            var mockResource = new Mock<IUnitOfWorkResource>();
            mockResource.Setup(r => r.ResourceIdentifier).Returns("test-resource");
            mockResource.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            uow.RegisterResource(mockResource.Object);
            await uow.CommitAsync(); // This should start transactions

            // Assert - Default isolation level should be ReadCommitted
            mockResource.Verify(r => r.BeginTransactionAsync(IsolationLevel.ReadCommitted, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Concurrent Resource Registration

        [Fact]
        public async Task Concurrent_ResourceRegistration_ShouldHandleThreadSafety()
        {
            // Arrange
            using var uow = CreateUnitOfWork();
            var mockResource1 = new Mock<IUnitOfWorkResource>();
            mockResource1.Setup(r => r.ResourceIdentifier).Returns("resource-1");
            mockResource1.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource1.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockResource2 = new Mock<IUnitOfWorkResource>();
            mockResource2.Setup(r => r.ResourceIdentifier).Returns("resource-2");
            mockResource2.Setup(r => r.HasActiveTransaction).Returns(false);
            mockResource2.Setup(r => r.BeginTransactionAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act - Register resources concurrently
            var task1 = Task.Run(() => uow.RegisterResource(mockResource1.Object));
            var task2 = Task.Run(() => uow.RegisterResource(mockResource2.Object));
            await Task.WhenAll(task1, task2);

            // Assert - Both resources should be registered
            Assert.Equal(2, uow.GetType().GetField("_resources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(uow) is List<IUnitOfWorkResource> resources ? resources.Count : 0);
        }

        #endregion

        #region UnitOfWorkManager

        [Fact]
        public void Manager_Current_Initially_Null()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            var provider = services.BuildServiceProvider();
            var manager = provider.GetRequiredService<IUnitOfWorkManager>();

            // Assert
            Assert.Null(manager.Current);
        }

        [Fact]
        public void Manager_Dispose_ClearsAfterAllDisposed()
        {
            // Arrange
            using (var outer = _uowManager.Begin())
            {
                Assert.Equal(outer.Id, _uowManager.Current?.Id);
            } // All disposed

            // Assert
            Assert.Null(_uowManager.Current); // Should be null after all disposed
        }

        #endregion
    }
}
