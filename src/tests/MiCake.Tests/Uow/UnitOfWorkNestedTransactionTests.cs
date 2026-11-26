using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for shared nested Unit of Work scenarios:
    /// parent-child relationships, rollback propagation, resource sharing, and savepoint delegation.
    /// </summary>
    public class UnitOfWorkNestedTransactionTests
    {
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWorkNestedTransactionTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
        }

        #region Nested UoW Creation Tests

        [Fact]
        public void NestedUoW_ShouldHaveParentReference()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);

            Assert.NotNull(child.Parent);
            Assert.Equal(parent.Id, child.Parent.Id);
            Assert.True(child.IsNested);
            Assert.False(parent.IsNested);
        }

        [Fact]
        public void MultiLevelNestedUoW_ShouldMaintainHierarchy()
        {
            var level1 = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var level2 = new UnitOfWork(_logger, new UnitOfWorkOptions(), level1);
            var level3 = new UnitOfWork(_logger, new UnitOfWorkOptions(), level2);

            Assert.Null(level1.Parent);
            Assert.Equal(level1.Id, level2.Parent?.Id);
            Assert.Equal(level2.Id, level3.Parent?.Id);
        }

        #endregion

        #region Nested UoW Commit Tests

        [Fact]
        public async Task NestedUoW_Commit_ShouldOnlyMarkAsCompleted()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();
            parent.RegisterResource(resource);

            await child.CommitAsync();

            Assert.True(child.IsCompleted);
            Assert.False(parent.IsCompleted);
            Assert.Equal(0, resource.CommitCount);
        }

        [Fact]
        public async Task NestedUoW_Commit_ShouldRaiseNoTransactionEvents()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var committingRaised = false;
            var committedRaised = false;
            var rollingBackRaised = false;
            var rolledBackRaised = false;
            child.OnCommitting += (s, e) => committingRaised = true;
            child.OnCommitted += (s, e) => committedRaised = true;
            child.OnRollingBack += (s, e) => rollingBackRaised = true;
            child.OnRolledBack += (s, e) => rolledBackRaised = true;

            await child.CommitAsync();

            Assert.False(committingRaised);
            Assert.False(committedRaised);
            Assert.False(rollingBackRaised);
            Assert.False(rolledBackRaised);
        }

        [Fact]
        public async Task ParentUoW_CommitAfterChildCommit_ShouldCommitResources()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();
            parent.RegisterResource(resource);

            await child.CommitAsync();
            await parent.CommitAsync();

            Assert.True(child.IsCompleted);
            Assert.True(parent.IsCompleted);
            Assert.Equal(1, resource.CommitCount);
        }

        #endregion

        #region Nested UoW Rollback Tests

        [Fact]
        public async Task NestedUoW_Rollback_ShouldMarkParentRollbackOnly()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();
            parent.RegisterResource(resource);

            await child.RollbackAsync();

            Assert.True(child.IsCompleted);
            Assert.False(parent.IsCompleted);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => parent.CommitAsync());
            Assert.Contains("rollback", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, resource.RollbackCount);
            Assert.Equal(0, resource.CommitCount);
        }

        [Fact]
        public async Task NestedUoW_Rollback_ShouldRaiseNoTransactionEvents()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var rollingBackRaised = false;
            var rolledBackRaised = false;
            child.OnRollingBack += (s, e) => rollingBackRaised = true;
            child.OnRolledBack += (s, e) => rolledBackRaised = true;

            await child.RollbackAsync();

            Assert.False(rollingBackRaised);
            Assert.False(rolledBackRaised);
        }

        [Fact]
        public async Task MultiLevelNested_MiddleLevelRollback_ShouldPropagateToRoot()
        {
            var root = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var middle = new UnitOfWork(_logger, new UnitOfWorkOptions(), root);
            var leaf = new UnitOfWork(_logger, new UnitOfWorkOptions(), middle);
            var resource = new TestUowResource();
            root.RegisterResource(resource);

            await leaf.CommitAsync();
            await middle.RollbackAsync();

            Assert.True(leaf.IsCompleted);
            Assert.True(middle.IsCompleted);

            await Assert.ThrowsAsync<InvalidOperationException>(() => root.CommitAsync());
            Assert.Equal(1, resource.RollbackCount);
        }

        #endregion

        #region Resource Registration in Nested UoW Tests

        [Fact]
        public void NestedUoW_RegisterResource_ShouldRegisterToParent()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();

            child.RegisterResource(resource);

            Assert.Equal(1, resource.PrepareCount);
            Assert.Equal(parent.Id, resource.PrepareContext!.UnitOfWorkId);
        }

        [Fact]
        public void MultipleNestedUoW_RegisteringSameResource_ShouldOnlyPrepareOnce()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child1 = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var child2 = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();

            child1.RegisterResource(resource);
            child2.RegisterResource(resource);

            Assert.Equal(1, resource.PrepareCount);
        }

        [Fact]
        public async Task NestedUoW_Flush_ShouldDelegateToParent()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();
            parent.RegisterResource(resource);

            var affected = await child.FlushAsync();

            Assert.Equal(1, affected);
            Assert.Equal(1, resource.FlushCount);
            Assert.False(parent.IsCompleted);
        }

        #endregion

        #region Savepoint Tests with Nested UoW

        [Fact]
        public async Task NestedUoW_CreateSavepoint_ShouldDelegateToParent()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();
            parent.RegisterResource(resource);

            var name = await child.CreateSavepointAsync("test_sp");

            Assert.Equal("test_sp", name);
            Assert.Contains("test_sp", resource.Savepoints);
        }

        [Fact]
        public async Task NestedUoW_RollbackToSavepoint_ShouldDelegateToParent()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();
            parent.RegisterResource(resource);
            await parent.CreateSavepointAsync("test_sp");

            await child.RollbackToSavepointAsync("test_sp");

            Assert.Contains("test_sp", resource.RolledBackTo);
        }

        #endregion

        #region Complex Nested Scenarios

        [Fact]
        public async Task ComplexScenario_MultipleChildrenCommit_ParentShouldCommitAll()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child1 = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var child2 = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource();
            child1.RegisterResource(resource1);
            child2.RegisterResource(resource2);

            await child1.CommitAsync();
            await child2.CommitAsync();
            await parent.CommitAsync();

            Assert.Equal(1, resource1.CommitCount);
            Assert.Equal(1, resource2.CommitCount);
        }

        [Fact]
        public async Task ComplexScenario_OneChildRollback_ShouldPreventParentCommit()
        {
            var parent = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var child1 = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var child2 = new UnitOfWork(_logger, new UnitOfWorkOptions(), parent);
            var resource = new TestUowResource();
            parent.RegisterResource(resource);

            await child1.CommitAsync();
            await child2.RollbackAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => parent.CommitAsync());
            Assert.Equal(0, resource.CommitCount);
        }

        #endregion
    }
}
