using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for resource registration: Prepare with the owning unit of work context,
    /// lazy activation on first use, and savepoint-driven activation.
    /// </summary>
    public class UnitOfWorkTwoPhaseRegistrationTests
    {
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWorkTwoPhaseRegistrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
        }

        #region Prepare Tests

        [Fact]
        public void RegisterResource_ShouldCallPrepareWithContext()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();

            uow.RegisterResource(resource);

            Assert.Equal(1, resource.PrepareCount);
            Assert.NotNull(resource.PrepareContext);
        }

        [Fact]
        public void RegisterResource_ShouldPassIsolationAndReadOnly()
        {
            var options = new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                IsReadOnly = true
            };
            var uow = new UnitOfWork(_logger, options, null);
            var resource = new TestUowResource();

            uow.RegisterResource(resource);

            Assert.Equal(IsolationLevel.Serializable, resource.PrepareContext!.IsolationLevel);
            Assert.True(resource.PrepareContext.IsReadOnly);
        }

        [Fact]
        public void RegisterResource_ShouldNotActivateTransaction()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();

            uow.RegisterResource(resource);

            Assert.Equal(0, resource.EnsureTransactionCount);
            Assert.False(resource.HasActiveTransaction);
        }

        [Fact]
        public void RegisterResource_PrepareFails_ShouldThrowAndNotRegister()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { PrepareException = new InvalidOperationException("Prepare failed") };

            Assert.Throws<InvalidOperationException>(() => uow.RegisterResource(resource));

            var other = new TestUowResource();
            uow.RegisterResource(other);
            // The failed resource must not consume a registration slot or state entry.
            Assert.Equal(0, resource.EnsureTransactionCount);
        }

        #endregion

        #region Activation Tests

        [Fact]
        public async Task ActivatePendingResourcesAsync_ShouldActivateAllRegisteredResources()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);

            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            Assert.Equal(1, resource1.EnsureTransactionCount);
            Assert.Equal(1, resource2.EnsureTransactionCount);
            Assert.True(uow.HasActiveTransactions);
        }

        [Fact]
        public async Task ActivatePendingResourcesAsync_CalledTwice_ShouldOnlyActivateOnce()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            Assert.Equal(1, resource.EnsureTransactionCount);
        }

        [Fact]
        public async Task ActivatePendingResourcesAsync_WithResourceFailure_ShouldThrowPrimary()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource { EnsureTransactionException = new InvalidOperationException("Activation failed") };
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync());

            Assert.Equal("Activation failed", ex.Message);
        }

        #endregion

        #region Savepoint Activation Tests

        [Fact]
        public async Task CreateSavepointAsync_InLazyMode_ShouldActivateTransactionsFirst()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await uow.CreateSavepointAsync("test_sp");

            Assert.Equal(1, resource.EnsureTransactionCount);
            Assert.Contains("test_sp", resource.Savepoints);
        }

        [Fact]
        public async Task RollbackToSavepointAsync_ShouldRollbackCoveredResources()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);
            await uow.CreateSavepointAsync("test_sp");

            await uow.RollbackToSavepointAsync("test_sp");

            Assert.Contains("test_sp", resource.RolledBackTo);
        }

        [Fact]
        public async Task CreateSavepointAsync_WithoutTransaction_ShouldThrow()
        {
            var uow = new UnitOfWork(_logger, UnitOfWorkOptions.ReadOnly, null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CreateSavepointAsync("test_sp"));
        }

        #endregion

        #region Nested UoW Registration Tests

        [Fact]
        public void NestedUoW_RegisterResource_ShouldPrepareWithParent()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();

            child.RegisterResource(resource);

            Assert.Equal(1, resource.PrepareCount);
            Assert.Equal(parent.Id, resource.PrepareContext!.UnitOfWorkId);
        }

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
    }
}
