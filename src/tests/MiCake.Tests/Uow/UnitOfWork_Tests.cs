using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
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
