using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for UnitOfWorkManager: ambient frames, shared nested units of work,
    /// lifecycle hooks, and isolated requiresNew execution.
    /// </summary>
    public class UnitOfWorkManagerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AmbientUnitOfWorkAccessor _ambientAccessor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly UnitOfWorkManager _manager;

        public UnitOfWorkManagerTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<AmbientUnitOfWorkAccessor>();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            services.AddScoped<IStandaloneUnitOfWorkExecutor, StandaloneUnitOfWorkExecutor>();
            _serviceProvider = services.BuildServiceProvider();

            _ambientAccessor = _serviceProvider.GetRequiredService<AmbientUnitOfWorkAccessor>();
            _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = _serviceProvider.GetRequiredService<ILogger<UnitOfWorkManager>>();
            _manager = new UnitOfWorkManager(_serviceProvider, _ambientAccessor, _scopeFactory, logger);
        }

        #region BeginAsync Tests

        [Fact]
        public async Task BeginAsync_WithDefaultOptions_ShouldCreateRootUnitOfWork()
        {
            var uow = await _manager.BeginAsync();

            Assert.NotNull(uow);
            Assert.NotEqual(Guid.Empty, uow.Id);
            Assert.Null(uow.Parent);
            Assert.False(uow.IsReadOnly);
            Assert.False(uow.IsCompleted);
        }

        [Fact]
        public async Task BeginAsync_WithIsolationLevel_ShouldCreateUnitOfWorkWithSpecifiedIsolationLevel()
        {
            var options = new UnitOfWorkOptions { IsolationLevel = IsolationLevel.Serializable };

            var uow = await _manager.BeginAsync(options);

            Assert.Equal(IsolationLevel.Serializable, uow.IsolationLevel);
        }

        [Fact]
        public async Task BeginAsync_WithReadOnlyOption_ShouldCreateReadOnlyUnitOfWork()
        {
            var uow = await _manager.BeginAsync(UnitOfWorkOptions.ReadOnly);

            Assert.True(uow.IsReadOnly);
        }

        [Fact]
        public async Task BeginAsync_WithExistingAmbientUow_ShouldCreateSharedNestedUnitOfWork()
        {
            using var outerUow = await _manager.BeginAsync();

            using var innerUow = await _manager.BeginAsync();

            Assert.NotNull(innerUow);
            Assert.Equal(outerUow.Id, innerUow.Parent?.Id);
            Assert.Same(outerUow, _manager.Current?.Parent);
        }

        [Fact]
        public async Task BeginAsync_CalledMultipleTimes_ShouldCreateNestedChain()
        {
            using var uow1 = await _manager.BeginAsync();
            using var uow2 = await _manager.BeginAsync();
            using var uow3 = await _manager.BeginAsync();

            Assert.Same(uow1, uow2.Parent);
            Assert.Same(uow2, uow3.Parent);
        }

        [Fact]
        public async Task BeginAsync_AfterCompletion_ShouldCreateNewRoot()
        {
            using (var uow1 = await _manager.BeginAsync())
            {
                await uow1.CommitAsync();
            }

            using var uow2 = await _manager.BeginAsync();

            Assert.Null(uow2.Parent);
            Assert.False(uow2.IsCompleted);
        }

        [Fact]
        public async Task BeginAsync_NestedWithDifferentOptions_ShouldInheritParentIsolationAndReadOnly()
        {
            using var outerUow = await _manager.BeginAsync(new UnitOfWorkOptions { IsolationLevel = IsolationLevel.ReadCommitted });

            using var innerUow = await _manager.BeginAsync(new UnitOfWorkOptions { IsolationLevel = IsolationLevel.Serializable });

            Assert.Equal(IsolationLevel.ReadCommitted, innerUow.IsolationLevel);
            Assert.Equal(outerUow.IsReadOnly, innerUow.IsReadOnly);
        }

        #endregion

        #region Current UoW Tests

        [Fact]
        public void Current_WithNoActiveUow_ShouldReturnNull()
        {
            Assert.Null(_manager.Current);
        }

        [Fact]
        public async Task Current_WithActiveUow_ShouldReturnActiveUow()
        {
            var uow = await _manager.BeginAsync();

            Assert.Same(uow, _manager.Current);

            await uow.MarkAsCompletedAsync();
        }

        [Fact]
        public async Task Current_AfterCompletion_ShouldReturnOuterOrNull()
        {
            var outer = await _manager.BeginAsync();
            var inner = await _manager.BeginAsync();
            Assert.Same(inner, _manager.Current);

            await inner.CommitAsync();
            Assert.Same(outer, _manager.Current);

            await outer.CommitAsync();
            Assert.Null(_manager.Current);
        }

        #endregion

        #region Parallel Ambient Flow Isolation Tests

        [Fact]
        public async Task ParallelFlows_BeginSameManager_ShouldNotLeakAmbientFrames()
        {
            // Two concurrent asynchronous flows share one manager instance; each flow must
            // see only its own ambient UoW (AsyncLocal execution-context isolation), and
            // completing one flow must not disturb the other's ambient frame.
            var uows = await Task.WhenAll(
                RunFlowAsync(),
                RunFlowAsync());

            Assert.NotSame(uows[0], uows[1]);

            async Task<IUnitOfWork> RunFlowAsync()
            {
                using var uow = await _manager.BeginAsync();
                Assert.Same(uow, _manager.Current);
                return uow;
            }
        }

        #endregion

        #region Lifecycle Hook Tests

        [Fact]
        public async Task BeginAsync_WithApplicableHook_ShouldInvokeHook()
        {
            var hookInvoked = false;
            var mockHook = new Mock<IUnitOfWorkLifetimeHook>();
            mockHook.Setup(h => h.ApplicableMode).Returns(TransactionInitializationMode.Immediate);
            mockHook.Setup(h => h.OnUnitOfWorkCreatedAsync(It.IsAny<IUnitOfWork>(), It.IsAny<UnitOfWorkOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() => hookInvoked = true)
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(mockHook.Object);
            var provider = services.BuildServiceProvider();
            var manager = new UnitOfWorkManager(
                provider,
                new AmbientUnitOfWorkAccessor(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<ILogger<UnitOfWorkManager>>());

            var uow = await manager.BeginAsync(new UnitOfWorkOptions { InitializationMode = TransactionInitializationMode.Immediate });

            Assert.True(hookInvoked);
        }

        [Fact]
        public async Task BeginAsync_WithNonApplicableHook_ShouldNotInvokeHook()
        {
            var hookInvoked = false;
            var mockHook = new Mock<IUnitOfWorkLifetimeHook>();
            mockHook.Setup(h => h.ApplicableMode).Returns(TransactionInitializationMode.Immediate);
            mockHook.Setup(h => h.OnUnitOfWorkCreatedAsync(It.IsAny<IUnitOfWork>(), It.IsAny<UnitOfWorkOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() => hookInvoked = true)
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(mockHook.Object);
            var provider = services.BuildServiceProvider();
            var manager = new UnitOfWorkManager(
                provider,
                new AmbientUnitOfWorkAccessor(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<ILogger<UnitOfWorkManager>>());

            var uow = await manager.BeginAsync(new UnitOfWorkOptions { InitializationMode = TransactionInitializationMode.Lazy });

            Assert.False(hookInvoked);
        }

        [Fact]
        public async Task BeginAsync_ImmediateMode_ShouldActivateResourcesRegisteredByHook()
        {
            var resource = new TestUowResource();
            var mockHook = new Mock<IUnitOfWorkLifetimeHook>();
            mockHook.Setup(h => h.ApplicableMode).Returns((TransactionInitializationMode?)null);
            mockHook.Setup(h => h.OnUnitOfWorkCreatedAsync(It.IsAny<IUnitOfWork>(), It.IsAny<UnitOfWorkOptions>(), It.IsAny<CancellationToken>()))
                .Callback((IUnitOfWork uow, UnitOfWorkOptions options, CancellationToken ct) =>
                    ((IUnitOfWorkInternal)uow).RegisterResource(resource))
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(mockHook.Object);
            var provider = services.BuildServiceProvider();
            var manager = new UnitOfWorkManager(
                provider,
                new AmbientUnitOfWorkAccessor(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<ILogger<UnitOfWorkManager>>());

            var uow = await manager.BeginAsync(new UnitOfWorkOptions { InitializationMode = TransactionInitializationMode.Immediate });

            Assert.Equal(1, resource.EnsureTransactionCount);
            Assert.True(uow.HasActiveTransactions);
        }

        #endregion

        #region ExecuteRequiresNewAsync Tests

        [Fact]
        public async Task ExecuteRequiresNewAsync_WithoutOuterUow_ShouldThrow()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _manager.ExecuteRequiresNewAsync((sp, ct) => Task.CompletedTask));
        }

        [Fact]
        public async Task ExecuteRequiresNewAsync_ShouldCommitInnerAndRestoreOuter()
        {
            using var outer = await _manager.BeginAsync();
            TestUowResource? innerResource = null;

            await _manager.ExecuteRequiresNewAsync(async (sp, ct) =>
            {
                var innerManager = sp.GetRequiredService<IUnitOfWorkManager>();
                var inner = innerManager.Current!;
                Assert.Null(inner.Parent);
                Assert.NotEqual(outer.Id, inner.Id);

                innerResource = new TestUowResource();
                ((IUnitOfWorkInternal)inner).RegisterResource(innerResource);
            });

            Assert.NotNull(innerResource);
            Assert.Equal(1, innerResource!.CommitCount);
            Assert.Equal(0, innerResource.RollbackCount);
            Assert.Same(outer, _manager.Current);
        }

        [Fact]
        public async Task ExecuteRequiresNewAsync_OperationFailure_ShouldRollbackInnerAndRestoreOuter()
        {
            using var outer = await _manager.BeginAsync();
            TestUowResource? innerResource = null;

            await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.ExecuteRequiresNewAsync(async (sp, ct) =>
            {
                var innerManager = sp.GetRequiredService<IUnitOfWorkManager>();
                var inner = innerManager.Current!;
                innerResource = new TestUowResource();
                ((IUnitOfWorkInternal)inner).RegisterResource(innerResource);

                throw new InvalidOperationException("boom");
            }));

            Assert.NotNull(innerResource);
            Assert.Equal(1, innerResource!.RollbackCount);
            Assert.Equal(0, innerResource.CommitCount);
            Assert.Same(outer, _manager.Current);
        }

        [Fact]
        public async Task ExecuteRequiresNewAsync_CommitFailure_ShouldRestoreOuter()
        {
            using var outer = await _manager.BeginAsync();

            // A single resource failing to commit means zero resources committed: the original
            // commit exception is preserved rather than reported as a partial commit.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.ExecuteRequiresNewAsync(async (sp, ct) =>
            {
                var innerManager = sp.GetRequiredService<IUnitOfWorkManager>();
                var inner = innerManager.Current!;
                var resource = new TestUowResource { CommitException = new InvalidOperationException("commit failed") };
                ((IUnitOfWorkInternal)inner).RegisterResource(resource);
            }));

            Assert.Equal("commit failed", ex.Message);
            Assert.Same(outer, _manager.Current);
        }

        [Fact]
        public async Task ExecuteRequiresNewAsync_OperationAndRollbackFailure_ShouldThrowBoundaryException()
        {
            using var outer = await _manager.BeginAsync();

            var ex = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => _manager.ExecuteRequiresNewAsync(async (sp, ct) =>
            {
                var innerManager = sp.GetRequiredService<IUnitOfWorkManager>();
                var inner = innerManager.Current!;
                var resource = new TestUowResource { RollbackException = new InvalidOperationException("rollback failed") };
                ((IUnitOfWorkInternal)inner).RegisterResource(resource);

                throw new InvalidOperationException("boom");
            }));

            Assert.IsType<InvalidOperationException>(ex.PrimaryException);
            Assert.Single(ex.RollbackExceptions);
            Assert.Same(outer, _manager.Current);
        }

        [Fact]
        public async Task ExecuteRequiresNewAsync_ShouldReturnResultAndCommit()
        {
            using var outer = await _manager.BeginAsync();
            TestUowResource? innerResource = null;

            var result = await _manager.ExecuteRequiresNewAsync(async (sp, ct) =>
            {
                var innerManager = sp.GetRequiredService<IUnitOfWorkManager>();
                var inner = innerManager.Current!;
                innerResource = new TestUowResource();
                ((IUnitOfWorkInternal)inner).RegisterResource(innerResource);
                return 42;
            });

            Assert.Equal(42, result);
            Assert.Equal(1, innerResource!.CommitCount);
            Assert.Same(outer, _manager.Current);
        }

        [Fact]
        public async Task ExecuteRequiresNewAsync_InnerBeginAsync_ShouldCreateNestedUnderInner()
        {
            using var outer = await _manager.BeginAsync();

            await _manager.ExecuteRequiresNewAsync(async (sp, ct) =>
            {
                var innerManager = sp.GetRequiredService<IUnitOfWorkManager>();
                using var nested = await innerManager.BeginAsync();
                Assert.NotNull(nested.Parent);
                Assert.Same(nested, innerManager.Current);
                Assert.Same(nested.Parent, innerManager.Current!.Parent);
            });
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public async Task ManagerDispose_ShouldNotDisposeAmbientUnitOfWork()
        {
            var uow = await _manager.BeginAsync();

            _manager.Dispose();

            Assert.False(uow.IsDisposed);

            await uow.MarkAsCompletedAsync();
        }

        #endregion
    }
}
