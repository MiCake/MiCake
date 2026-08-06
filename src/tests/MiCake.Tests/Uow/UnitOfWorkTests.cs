using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for UnitOfWork core functionality.
    /// Covers registration, flush, commit, partial commit, rollback, savepoints, events, and disposal.
    /// </summary>
    public class UnitOfWorkTests
    {
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWorkTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateUnitOfWork()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);

            Assert.NotNull(uow);
            Assert.NotEqual(Guid.Empty, uow.Id);
            Assert.False(uow.IsCompleted);
            Assert.False(uow.IsReadOnly);
            Assert.Null(uow.Parent);
        }

        [Fact]
        public void Constructor_WithParent_ShouldSetParentReference()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);

            Assert.Same(parent, child.Parent);
            Assert.True(child.IsNested);
        }

        #endregion

        #region Resource Registration Tests

        [Fact]
        public void RegisterResource_ShouldPrepareWithUnitOfWorkContext()
        {
            var options = new UnitOfWorkOptions { IsolationLevel = IsolationLevel.Serializable };
            var uow = new UnitOfWork(_logger, options, null);
            var resource = new TestUowResource();

            uow.RegisterResource(resource);

            Assert.Equal(1, resource.PrepareCount);
            Assert.NotNull(resource.PrepareContext);
            Assert.Equal(uow.Id, resource.PrepareContext.UnitOfWorkId);
            Assert.Equal(IsolationLevel.Serializable, resource.PrepareContext.IsolationLevel);
            Assert.False(resource.PrepareContext.IsReadOnly);
            Assert.Equal(0, resource.EnsureTransactionCount);
        }

        [Fact]
        public void RegisterResource_WithReadOnlyUow_ShouldPassReadOnlyFlag()
        {
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.ReadOnly, null);
            var resource = new TestUowResource();

            uow.RegisterResource(resource);

            Assert.True(resource.PrepareContext!.IsReadOnly);
        }

        [Fact]
        public void RegisterResource_SameResourceTwice_ShouldOnlyPrepareOnce()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();

            uow.RegisterResource(resource);
            uow.RegisterResource(resource);

            Assert.Equal(1, resource.PrepareCount);
        }

        [Fact]
        public void RegisterResource_WithNestedUow_ShouldRegisterToParent()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();

            child.RegisterResource(resource);

            Assert.Equal(1, resource.PrepareCount);
            Assert.Equal(parent.Id, resource.PrepareContext!.UnitOfWorkId);
        }

        [Fact]
        public void RegisterResource_PrepareFails_ShouldThrowAndNotRegister()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { PrepareException = new InvalidOperationException("Prepare failed") };

            Assert.Throws<InvalidOperationException>(() => uow.RegisterResource(resource));
        }

        #endregion

        #region FlushAsync Tests

        [Fact]
        public async Task FlushAsync_ShouldActivateAndFlushResourcesInRegistrationOrder()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);

            var affected = await uow.FlushAsync();

            Assert.Equal(2, affected);
            Assert.Equal(1, resource1.EnsureTransactionCount);
            Assert.Equal(1, resource1.FlushCount);
            Assert.Equal(1, resource2.EnsureTransactionCount);
            Assert.Equal(1, resource2.FlushCount);
            Assert.False(uow.IsCompleted);
            Assert.True(uow.HasActiveTransactions);
        }

        [Fact]
        public async Task FlushAsync_ShouldNotCommitOrRaiseEvents()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);
            var committingRaised = false;
            var committedRaised = false;
            uow.OnCommitting += (s, e) => committingRaised = true;
            uow.OnCommitted += (s, e) => committedRaised = true;

            await uow.FlushAsync();

            Assert.False(committingRaised);
            Assert.False(committedRaised);
            Assert.Equal(0, resource.CommitCount);
        }

        [Fact]
        public async Task FlushAsync_InReadOnlyMode_ShouldReject()
        {
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.ReadOnly, null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.FlushAsync());
            Assert.Equal(0, resource.FlushCount);
        }

        [Fact]
        public async Task FlushAsync_WithResourceFailure_ShouldMarkRollbackOnlyAndStop()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource { FlushException = new InvalidOperationException("Flush failed") };
            var resource3 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);
            uow.RegisterResource(resource3);

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.FlushAsync());

            Assert.Equal(1, resource1.FlushCount);
            Assert.Equal(1, resource2.FlushCount);
            Assert.Equal(0, resource3.FlushCount);
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
        }

        [Fact]
        public async Task FlushAsync_WithoutResources_ShouldReturnZero()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);

            var affected = await uow.FlushAsync();

            Assert.Equal(0, affected);
        }

        #endregion

        #region CommitAsync Tests

        [Fact]
        public async Task CommitAsync_ShouldActivateFlushThenCommitInOrder()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await uow.CommitAsync();

            Assert.True(uow.IsCompleted);
            Assert.Equal(1, resource.EnsureTransactionCount);
            Assert.Equal(1, resource.FlushCount);
            Assert.Equal(1, resource.CommitCount);
        }

        [Fact]
        public async Task CommitAsync_WithoutResources_ShouldCompleteWithoutEvents()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var committingRaised = false;
            var committedRaised = false;
            uow.OnCommitting += (s, e) => committingRaised = true;
            uow.OnCommitted += (s, e) => committedRaised = true;

            await uow.CommitAsync();

            Assert.True(uow.IsCompleted);
            Assert.False(committingRaised);
            Assert.False(committedRaised);
        }

        [Fact]
        public async Task CommitAsync_InReadOnlyMode_ShouldCompleteWithoutActivation()
        {
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.ReadOnly, null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await uow.CommitAsync();

            Assert.True(uow.IsCompleted);
            Assert.Equal(0, resource.EnsureTransactionCount);
            Assert.Equal(0, resource.CommitCount);
        }

        [Fact]
        public async Task CommitAsync_AlreadyCompleted_ShouldThrow()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            await uow.CommitAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
        }

        [Fact]
        public async Task CommitAsync_ShouldRaiseOnCommittingAndOnCommitted()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);
            var committingRaised = false;
            var committedRaised = false;
            uow.OnCommitting += (s, e) => { committingRaised = true; Assert.Equal(uow.Id, e.UnitOfWorkId); };
            uow.OnCommitted += (s, e) => { committedRaised = true; Assert.Equal(uow.Id, e.UnitOfWorkId); };

            await uow.CommitAsync();

            Assert.True(committingRaised);
            Assert.True(committedRaised);
        }

        [Fact]
        public async Task CommitAsync_OnCommittingHandlerFailure_ShouldAbortCommitAndRollback()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);
            uow.OnCommitting += (s, e) => throw new InvalidOperationException("Handler failed");

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());

            Assert.Equal(0, resource.CommitCount);
            Assert.Equal(1, resource.RollbackCount);
            Assert.False(uow.IsCompleted);
        }

        [Fact]
        public async Task CommitAsync_MarkedRollbackOnly_ShouldRollbackAndThrow()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);
            ((UnitOfWork)uow).MarkRollbackOnly();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());

            Assert.Contains("rollback", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, resource.RollbackCount);
            Assert.Equal(0, resource.CommitCount);
        }

        [Fact]
        public async Task CommitAsync_PartialFailure_ShouldExposeFinalOutcomeAndRollbackEligibleResources()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource { CommitException = new InvalidOperationException("Commit failed") };
            var resource3 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);
            uow.RegisterResource(resource3);

            var ex = await Assert.ThrowsAsync<PartialUnitOfWorkCommitException>(() => uow.CommitAsync());

            Assert.Equal(1, resource1.CommitCount);
            Assert.Equal(1, resource2.CommitCount);
            Assert.Equal(0, resource3.CommitCount);
            Assert.Equal(1, resource2.RollbackCount);
            Assert.Equal(1, resource3.RollbackCount);
            Assert.Equal(0, resource1.RollbackCount);

            var outcome = ex.Outcome;
            Assert.Equal(uow.Id, outcome.UnitOfWorkId);
            Assert.Equal(3, outcome.Resources.Count);
            Assert.Equal(UnitOfWorkResourceCommitState.Committed, outcome.Resources[0].CommitState);
            Assert.Null(outcome.Resources[0].RollbackState);
            Assert.Equal(UnitOfWorkResourceCommitState.Failed, outcome.Resources[1].CommitState);
            Assert.Equal(UnitOfWorkResourceCommitState.RolledBack, outcome.Resources[1].RollbackState);
            Assert.Equal(UnitOfWorkResourceCommitState.Pending, outcome.Resources[2].CommitState);
            Assert.Equal(UnitOfWorkResourceCommitState.RolledBack, outcome.Resources[2].RollbackState);
            Assert.Single(ex.CommitFailures);
            Assert.Empty(ex.RollbackFailures);
            Assert.False(uow.IsCompleted);
        }

        [Fact]
        public async Task CommitAsync_CommitAndRollbackFailure_ShouldExposeBoth()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource
            {
                CommitException = new InvalidOperationException("Commit failed"),
                RollbackException = new InvalidOperationException("Rollback failed")
            };
            uow.RegisterResource(resource);

            // Zero resources committed: this is a plain commit failure combined with a rollback
            // failure, not a partial commit.
            var ex = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => uow.CommitAsync());

            Assert.IsType<InvalidOperationException>(ex.PrimaryException);
            Assert.Single(ex.RollbackExceptions);
        }

        [Fact]
        public async Task CommitAsync_FlushFailure_ShouldRollbackEligibleResources()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource { FlushException = new InvalidOperationException("Flush failed") };
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());

            Assert.Equal("Flush failed", ex.Message);
            Assert.Equal(1, resource1.RollbackCount);
            Assert.Equal(1, resource2.RollbackCount);
            Assert.Equal(0, resource1.CommitCount);
        }

        #endregion

        #region RollbackAsync Tests

        [Fact]
        public async Task RollbackAsync_ShouldRollbackAllResourcesAndRaiseEvents()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            var rollingBackRaised = false;
            var rolledBackRaised = false;
            uow.OnRollingBack += (s, e) => rollingBackRaised = true;
            uow.OnRolledBack += (s, e) => { rolledBackRaised = true; Assert.Equal(uow.Id, e.UnitOfWorkId); };

            await uow.RollbackAsync();

            Assert.True(uow.IsCompleted);
            Assert.Equal(1, resource1.RollbackCount);
            Assert.Equal(1, resource2.RollbackCount);
            Assert.True(rollingBackRaised);
            Assert.True(rolledBackRaised);
        }

        [Fact]
        public async Task RollbackAsync_WithResourceFailure_ShouldThrowBoundaryExceptionAndNotRaiseOnRolledBack()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource { RollbackException = new InvalidOperationException("Rollback failed") };
            var resource2 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            var rolledBackRaised = false;
            uow.OnRolledBack += (s, e) => rolledBackRaised = true;

            var ex = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => uow.RollbackAsync());

            Assert.Single(ex.RollbackExceptions);
            Assert.Equal(1, resource2.RollbackCount);
            Assert.False(rolledBackRaised);
            Assert.False(uow.IsCompleted);
        }

        [Fact]
        public async Task RollbackAsync_WithoutResources_ShouldComplete()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);

            await uow.RollbackAsync();

            Assert.True(uow.IsCompleted);
        }

        #endregion

        #region Savepoint Tests

        [Fact]
        public async Task CreateSavepointAsync_ShouldActivateResourcesFirst()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            var name = await uow.CreateSavepointAsync("test_sp");

            Assert.Equal("test_sp", name);
            Assert.Equal(1, resource.EnsureTransactionCount);
            Assert.Contains("test_sp", resource.Savepoints);
        }

        [Fact]
        public async Task CreateSavepointAsync_WithNullName_ShouldGenerateName()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            var name = await uow.CreateSavepointAsync(null!);

            Assert.NotNull(name);
            Assert.Single(resource.Savepoints);
        }

        [Fact]
        public async Task CreateSavepointAsync_WithoutSavepointSupport_ShouldThrowBeforeChangingState()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { SupportsSavepoints = false };
            uow.RegisterResource(resource);
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            await Assert.ThrowsAsync<NotSupportedException>(() => uow.CreateSavepointAsync("test_sp"));

            Assert.Empty(resource.Savepoints);
        }

        [Fact]
        public async Task RollbackToSavepointAsync_WithLateRegisteredResource_ShouldFailBeforeChangingState()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            uow.RegisterResource(resource1);
            await uow.CreateSavepointAsync("test_sp");

            var resource2 = new TestUowResource();
            uow.RegisterResource(resource2);

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackToSavepointAsync("test_sp"));

            Assert.Empty(resource1.RolledBackTo);
            Assert.Empty(resource2.RolledBackTo);
        }

        [Fact]
        public async Task RollbackToSavepointAsync_ShouldRollbackAllCoveredResources()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);
            await uow.CreateSavepointAsync("test_sp");

            await uow.RollbackToSavepointAsync("test_sp");

            Assert.Contains("test_sp", resource1.RolledBackTo);
            Assert.Contains("test_sp", resource2.RolledBackTo);
        }

        [Fact]
        public async Task ReleaseSavepointAsync_ShouldRemoveCoverage()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);
            await uow.CreateSavepointAsync("test_sp");

            await uow.ReleaseSavepointAsync("test_sp");

            Assert.Contains("test_sp", resource.Released);
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackToSavepointAsync("test_sp"));
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldNotThrowAndDisposeResources()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            uow.Dispose();

            Assert.True(uow.IsDisposed);
            Assert.True(uow.IsCompleted);
            Assert.Equal(1, resource.DisposeCount);
        }

        [Fact]
        public async Task DisposeAsync_WithActiveTransactions_ShouldRollbackBestEffort()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            await uow.DisposeAsync();

            Assert.True(uow.IsDisposed);
            Assert.Equal(1, resource.RollbackCount);
            Assert.Equal(1, resource.DisposeCount);
        }

        #endregion

        #region MarkAsCompleted Tests

        [Fact]
        public async Task MarkAsCompleted_ShouldSetIsCompletedToTrue()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);

            await uow.MarkAsCompletedAsync();

            Assert.True(uow.IsCompleted);
        }

        #endregion
    }
}
