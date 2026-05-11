using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for nested Unit of Work scenarios
    /// Tests cover parent-child relationships, rollback propagation, and resource sharing
    /// </summary>
    public class UnitOfWorkNestedTransactionTests
    {
        private readonly ILogger<UnitOfWork> _uowLogger;
        private readonly ILogger<UnitOfWorkManager> _managerLogger;
        private readonly IServiceProvider _serviceProvider;
        private readonly UnitOfWorkManager _manager;

        public UnitOfWorkNestedTransactionTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _uowLogger = loggerFactory.CreateLogger<UnitOfWork>();
            _managerLogger = loggerFactory.CreateLogger<UnitOfWorkManager>();
            
            var services = new ServiceCollection();
            services.AddSingleton(_uowLogger);
            services.AddSingleton(_managerLogger);
            _serviceProvider = services.BuildServiceProvider();
            
            _manager = new UnitOfWorkManager(_serviceProvider, _managerLogger);
        }

        #region Nested UoW Creation Tests

        [Fact]
        public async Task NestedUoW_ShouldHaveParentReference()
        {
            // Arrange
            using var parent = await _manager.BeginAsync();

            // Act
            using var child = await _manager.BeginAsync();

            // Assert
            Assert.NotNull(child.Parent);
            Assert.Equal(parent.Id, child.Parent.Id);
            Assert.True(child.IsNested);
        }

        [Fact]
        public async Task MultiLevelNestedUoW_ShouldMaintainHierarchy()
        {
            // Arrange & Act
            using var level1 = await _manager.BeginAsync();
            using var level2 = await _manager.BeginAsync();
            using var level3 = await _manager.BeginAsync();

            // Assert
            Assert.Null(level1.Parent);
            Assert.Equal(level1.Id, level2.Parent?.Id);
            Assert.Equal(level2.Id, level3.Parent?.Id);
        }

        [Fact]
        public async Task NestedUoW_WithRequiresNew_ShouldCreateNewRoot()
        {
            // Arrange
            using var parent = await _manager.BeginAsync();

            // Act
            using var newRoot = await _manager.BeginAsync(requiresNew: true);

            // Assert
            Assert.Null(newRoot.Parent);
            Assert.False(newRoot.IsNested);
            Assert.NotEqual(parent.Id, newRoot.Id);
        }

        #endregion

        #region Nested UoW Commit Tests

        [Fact]
        public async Task NestedUoW_Commit_ShouldOnlyMarkAsCompleted()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();
            parent.RegisterResource(mockResource.Object);

            // Act
            await child.CommitAsync();

            // Assert
            Assert.True(child.IsCompleted);
            Assert.False(parent.IsCompleted);
            // Resource should not be committed by child
            mockResource.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ParentUoW_CommitAfterChildCommit_ShouldCommitResources()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();
            mockResource.Setup(r => r.HasActiveTransaction).Returns(true);
            parent.RegisterResource(mockResource.Object);

            // Act
            await child.CommitAsync();
            await parent.CommitAsync();

            // Assert
            Assert.True(child.IsCompleted);
            Assert.True(parent.IsCompleted);
            mockResource.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Nested UoW Rollback Tests

        [Fact]
        public async Task NestedUoW_Rollback_ShouldMarkParentForRollback()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();
            mockResource.Setup(r => r.HasActiveTransaction).Returns(true);
            parent.RegisterResource(mockResource.Object);

            // Act
            await child.RollbackAsync();

            // Assert
            Assert.True(child.IsCompleted);
            
            // Parent commit should fail because child rolled back
            await Assert.ThrowsAsync<InvalidOperationException>(() => parent.CommitAsync());
        }

        [Fact]
        public async Task ParentUoW_CommitAfterChildRollback_ShouldThrowException()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();
            mockResource.Setup(r => r.HasActiveTransaction).Returns(true);
            parent.RegisterResource(mockResource.Object);

            // Act
            await child.RollbackAsync();

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => parent.CommitAsync());
            Assert.Contains("rollback", exception.Message.ToLower());
        }

        [Fact]
        public async Task MultiLevelNested_MiddleLevelRollback_ShouldPropagateToRoot()
        {
            // Arrange
            var root = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var middle = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, root);
            var leaf = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, middle);
            
            var mockResource = CreateMockResource();
            mockResource.Setup(r => r.HasActiveTransaction).Returns(true);
            root.RegisterResource(mockResource.Object);

            // Act
            await leaf.CommitAsync();  // Leaf commits
            await middle.RollbackAsync();  // Middle rolls back

            // Assert
            Assert.True(leaf.IsCompleted);
            Assert.True(middle.IsCompleted);
            
            // Root should fail to commit
            await Assert.ThrowsAsync<InvalidOperationException>(() => root.CommitAsync());
        }

        #endregion

        #region Resource Registration in Nested UoW Tests

        [Fact]
        public void NestedUoW_RegisterResource_ShouldRegisterToParent()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();

            // Act
            child.RegisterResource(mockResource.Object);

            // Assert
            mockResource.Verify(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()), Times.Once);
        }

        [Fact]
        public void MultipleNestedUoW_RegisteringSameResource_ShouldOnlyRegisterOnce()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child1 = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            var child2 = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();

            // Act
            child1.RegisterResource(mockResource.Object);
            child2.RegisterResource(mockResource.Object);

            // Assert - Should only prepare once (deduplicated by resource identifier)
            mockResource.Verify(r => r.PrepareForTransaction(It.IsAny<UnitOfWorkOptions>()), Times.Once);
        }

        #endregion

        #region Savepoint Tests with Nested UoW

        [Fact]
        public async Task NestedUoW_CreateSavepoint_ShouldDelegateToParent()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();
            parent.RegisterResource(mockResource.Object);

            // Act
            var savepointName = await child.CreateSavepointAsync("test_sp");

            // Assert
            Assert.Equal("test_sp", savepointName);
            mockResource.Verify(
                r => r.CreateSavepointAsync("test_sp", It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task NestedUoW_RollbackToSavepoint_ShouldDelegateToParent()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();
            parent.RegisterResource(mockResource.Object);
            
            var savepointName = await parent.CreateSavepointAsync("test_sp");

            // Act
            await child.RollbackToSavepointAsync(savepointName);

            // Assert
            mockResource.Verify(
                r => r.RollbackToSavepointAsync("test_sp", It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        #endregion

        #region Complex Nested Scenarios

        [Fact]
        public async Task ComplexScenario_MultipleChildrenCommit_ParentShouldCommitAll()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child1 = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            var child2 = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource1 = CreateMockResource();
            var mockResource2 = CreateMockResource();
            mockResource1.Setup(r => r.HasActiveTransaction).Returns(true);
            mockResource2.Setup(r => r.HasActiveTransaction).Returns(true);
            
            child1.RegisterResource(mockResource1.Object);
            child2.RegisterResource(mockResource2.Object);

            // Act
            await child1.CommitAsync();
            await child2.CommitAsync();
            await parent.CommitAsync();

            // Assert
            mockResource1.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockResource2.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ComplexScenario_OneChildRollback_ShouldPreventParentCommit()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child1 = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            var child2 = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var mockResource = CreateMockResource();
            mockResource.Setup(r => r.HasActiveTransaction).Returns(true);
            parent.RegisterResource(mockResource.Object);

            // Act
            await child1.CommitAsync();
            await child2.RollbackAsync();  // One child rolls back

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => parent.CommitAsync());
            mockResource.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Event Propagation Tests

        [Fact]
        public async Task NestedUoW_CommitEvents_ShouldRaiseForNestedUoW()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var committingRaised = false;
            var committedRaised = false;

            child.OnCommitting += (sender, args) =>
            {
                committingRaised = true;
                Assert.True(args.IsNested);
            };

            child.OnCommitted += (sender, args) =>
            {
                committedRaised = true;
                Assert.True(args.IsNested);
            };

            // Act
            await child.CommitAsync();

            // Assert
            Assert.True(committingRaised);
            Assert.True(committedRaised);
        }

        [Fact]
        public async Task NestedUoW_RollbackEvents_ShouldRaiseForNestedUoW()
        {
            // Arrange
            var parent = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, null);
            var child = new UnitOfWork(_uowLogger, UnitOfWorkOptions.Default, parent);
            
            var rolledBackRaised = false;

            child.OnRolledBack += (sender, args) =>
            {
                rolledBackRaised = true;
                Assert.True(args.IsNested);
            };

            // Act
            await child.RollbackAsync();

            // Assert
            Assert.True(rolledBackRaised);
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
            mock.Setup(r => r.RollbackToSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mock.Setup(r => r.ReleaseSavepointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            return mock;
        }

        #endregion
    }
}
