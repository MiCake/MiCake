using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for transaction activation: lazy activation, immediate activation,
    /// idempotency, read-only rejection, and nested delegation.
    /// </summary>
    public class UnitOfWorkImmediateModeTests
    {
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWorkImmediateModeTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
        }

        #region Activation Tests

        [Fact]
        public async Task ActivatePendingResources_WithMultipleResources_ShouldActivateAll()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource();
            var resource3 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);
            uow.RegisterResource(resource3);

            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            Assert.Equal(1, resource1.EnsureTransactionCount);
            Assert.Equal(1, resource2.EnsureTransactionCount);
            Assert.Equal(1, resource3.EnsureTransactionCount);
            Assert.True(uow.HasActiveTransactions);
        }

        [Fact]
        public async Task ActivatePendingResources_CalledMultipleTimes_ShouldOnlyActivateOnce()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            Assert.Equal(1, resource.EnsureTransactionCount);
        }

        [Fact]
        public async Task ActivatePendingResources_WithoutResources_ShouldBeNoOp()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);

            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            Assert.False(uow.HasActiveTransactions);
        }

        [Fact]
        public async Task ActivatePendingResources_InReadOnlyMode_ShouldReject()
        {
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.ReadOnly, null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync());

            Assert.Equal(0, resource.EnsureTransactionCount);
            Assert.False(uow.HasActiveTransactions);
        }

        [Fact]
        public async Task ActivatePendingResources_WithResourceFailure_ShouldThrowAndMarkRollbackOnly()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource { EnsureTransactionException = new InvalidOperationException("Activation failed") };
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync());

            Assert.Equal(1, resource1.EnsureTransactionCount);
            // resource1 really holds an active transaction even though activation failed partway;
            // the UoW must reflect that instead of hiding the real state.
            Assert.True(uow.HasActiveTransactions);
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
        }

        #endregion

        #region Immediate Mode with Nested UoW Tests

        [Fact]
        public async Task NestedUoW_ActivatePendingResources_ShouldDelegateToParent()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();
            child.RegisterResource(resource);

            await ((IUnitOfWorkInternal)child).ActivatePendingResourcesAsync();

            Assert.Equal(1, resource.EnsureTransactionCount);
            Assert.True(parent.HasActiveTransactions);
        }

        #endregion

        #region Immediate Mode Commit Tests

        [Fact]
        public async Task CommitAsync_AfterPreActivation_ShouldNotActivateAgain()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();
            await uow.CommitAsync();

            Assert.Equal(1, resource.EnsureTransactionCount);
            Assert.Equal(1, resource.CommitCount);
        }

        #endregion
    }
}
