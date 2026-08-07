using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Regression tests capturing the t1 review findings (2026-08-06).
    /// Each test documents the expected behavior per ADR-007 / ADR-011 and the resource contract.
    /// The failure-path tests currently fail and are the red phase for /mvt-fix.
    /// </summary>
    public class UnitOfWorkReviewRegressionTests
    {
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWorkReviewRegressionTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<UnitOfWork>();
        }

        private static UnitOfWorkManager CreateManager(out IServiceProvider provider)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<AmbientUnitOfWorkAccessor>();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            provider = services.BuildServiceProvider();

            return new UnitOfWorkManager(
                provider,
                provider.GetRequiredService<AmbientUnitOfWorkAccessor>(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<ILogger<UnitOfWorkManager>>());
        }

        #region C1: Late-registered resource must be transaction-activated before flush

        [Fact]
        public async Task FlushAsync_AfterFirstFlush_ShouldActivateLateRegisteredResourceBeforeFlushingIt()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var first = new TestUowResource();
            var late = new TestUowResource();
            uow.RegisterResource(first);

            await uow.FlushAsync();
            uow.RegisterResource(late);
            await uow.FlushAsync();

            // ADR-011: every writable resource is activated before its first supported write.
            Assert.Equal(1, late.EnsureTransactionCount);
            Assert.True(late.HasActiveTransaction);
            Assert.Equal(1, late.FlushCount);
        }

        #endregion

        #region W1: HasActiveTransactions must reflect any resource with a real active transaction

        [Fact]
        public async Task ActivatePendingResources_PartialFailure_ShouldReportAnyActiveResourceTransaction()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource { EnsureTransactionException = new InvalidOperationException("Activation failed") };
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync());

            // resource1 really holds an active transaction; the UoW must not hide it.
            Assert.True(resource1.HasActiveTransaction);
            Assert.True(uow.HasActiveTransactions);
        }

        #endregion

        #region C2: requiresNew inner UoW must run the same initialization pipeline as BeginAsync

        [Fact]
        public async Task ExecuteRequiresNewAsync_WithImmediateOptions_ShouldRunLifecycleHooksForInnerUnitOfWork()
        {
            var hookInvoked = false;
            var mockHook = new Mock<IUnitOfWorkLifetimeHook>();
            mockHook.Setup(h => h.ApplicableMode).Returns(TransactionInitializationMode.Immediate);
            mockHook.Setup(h => h.OnUnitOfWorkCreatedAsync(It.IsAny<IUnitOfWork>(), It.IsAny<UnitOfWorkOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() => hookInvoked = true)
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<AmbientUnitOfWorkAccessor>();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            services.AddSingleton(mockHook.Object);
            var provider = services.BuildServiceProvider();
            var manager = new UnitOfWorkManager(
                provider,
                provider.GetRequiredService<AmbientUnitOfWorkAccessor>(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<ILogger<UnitOfWorkManager>>());

            using var outer = await manager.BeginAsync(); // Lazy: immediate-only hook must not run for outer
            Assert.False(hookInvoked);

            await manager.ExecuteRequiresNewAsync(
                (sp, ct) => Task.CompletedTask,
                new UnitOfWorkOptions { InitializationMode = TransactionInitializationMode.Immediate });

            // The isolated inner UoW goes through the same hook/activation pipeline as BeginAsync.
            Assert.True(hookInvoked);
        }

        #endregion

        #region C3: Partial commit outcome must retain the identity of the failed resource

        [Fact]
        public async Task CommitAsync_PartialFailure_ShouldExposeWhichResourceFailedToCommit()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource { CommitException = new InvalidOperationException("Commit failed") };
            var resource3 = new TestUowResource();
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);
            uow.RegisterResource(resource3);

            var ex = await Assert.ThrowsAsync<PartialUnitOfWorkCommitException>(() => uow.CommitAsync());

            // The outcome must keep the failed-commit identity distinct from never-committed resources.
            Assert.Contains(ex.Outcome.Resources,
                r => r.ResourceId == resource2.Id && r.CommitState == UnitOfWorkResourceCommitState.Failed);
        }

        [Fact]
        public async Task CommitAsync_FirstResourceCommitFailure_ShouldNotReportPartialCommitWithoutAnyCommittedResource()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { CommitException = new InvalidOperationException("Commit failed") };
            uow.RegisterResource(resource);

            // PartialUnitOfWorkCommitException documents "at least one resource committed";
            // with zero committed resources the primary exception semantics must be preserved.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
            Assert.Equal("Commit failed", ex.Message);
        }

        #endregion

        #region C4: A partial commit is never reported as a successful overall rollback

        [Fact]
        public async Task ExecuteRequiresNewAsync_PartialCommitFailure_ShouldNotRaiseOnRolledBack()
        {
            var manager = CreateManager(out _);

            using var outer = await manager.BeginAsync();
            var rolledBackRaised = false;

            await Assert.ThrowsAsync<PartialUnitOfWorkCommitException>(() => manager.ExecuteRequiresNewAsync(async (sp, ct) =>
            {
                var inner = sp.GetRequiredService<IUnitOfWorkManager>().Current!;
                inner.OnRolledBack += (s, e) => rolledBackRaised = true;
                ((IUnitOfWorkInternal)inner).RegisterResource(new TestUowResource());
                ((IUnitOfWorkInternal)inner).RegisterResource(new TestUowResource
                {
                    CommitException = new InvalidOperationException("Commit failed")
                });
            }));

            // ADR-007: never report the overall UoW as rolled back after any resource committed.
            Assert.False(rolledBackRaised);
            Assert.Same(outer, manager.Current);
        }

        #endregion

        #region C5: Disposal must surface rollback and cleanup failures

        [Fact]
        public async Task DisposeAsync_WithRollbackFailure_ShouldSurfaceBoundaryException()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { RollbackException = new InvalidOperationException("Rollback failed") };
            uow.RegisterResource(resource);
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            var ex = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => uow.DisposeAsync().AsTask());

            Assert.Contains(ex.RollbackExceptions, e => e.Message == "Rollback failed");
        }

        [Fact]
        public async Task DisposeAsync_WithResourceDisposeFailure_ShouldSurfaceCleanupException()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { DisposeException = new InvalidOperationException("Dispose failed") };
            uow.RegisterResource(resource);
            await uow.CommitAsync();

            var ex = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => uow.DisposeAsync().AsTask());

            Assert.Contains(ex.CleanupExceptions, e => e.Message == "Dispose failed");
        }

        #endregion

        #region W2: Cancellation must not be reused for cleanup rollback

        [Fact]
        public async Task ExecuteRequiresNewAsync_CancelledOperation_ShouldPreserveOriginalCancellation()
        {
            var manager = CreateManager(out _);

            using var outer = await manager.BeginAsync();
            using var cts = new CancellationTokenSource();

            // The resource models a real provider that honors the token during rollback.
            await Assert.ThrowsAsync<OperationCanceledException>(() => manager.ExecuteRequiresNewAsync(
                async (sp, ct) =>
                {
                    var inner = sp.GetRequiredService<IUnitOfWorkManager>().Current!;
                    ((IUnitOfWorkInternal)inner).RegisterResource(new TestUowResource { ThrowIfCanceled = true });
                    cts.Cancel();
                    ct.ThrowIfCancellationRequested();
                },
                cancellationToken: cts.Token));

            // The original cancellation semantics must be preserved and the outer frame restored.
            Assert.Same(outer, manager.Current);
        }

        [Fact]
        public async Task CommitAsync_Cancelled_ShouldPropagateCancellationAndNotComplete()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { ThrowIfCanceled = true };
            uow.RegisterResource(resource);
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // The primary cancellation propagates unchanged (compensation rollback is not
            // cancelled by the operation's token), and the resource was never committed.
            await Assert.ThrowsAsync<OperationCanceledException>(() => uow.CommitAsync(cts.Token));

            Assert.Equal(0, resource.CommitCount);

            // The cancelled commit must not report the UoW as completed: the caller can
            // observe that the boundary did not finish.
            Assert.False(uow.IsCompleted);
        }

        [Fact]
        public async Task FlushAsync_Cancelled_ShouldSurfaceCancellationAndMarkRollbackOnly()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { ThrowIfCanceled = true };
            uow.RegisterResource(resource);
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // The resource records the flush attempt before honoring the token, so the
            // flush was attempted and cancelled; the UoW must surface the cancellation and
            // never report the flush as completed.
            await Assert.ThrowsAsync<OperationCanceledException>(() => uow.FlushAsync(cts.Token));

            Assert.Equal(1, resource.FlushCount);

            // Cancellation during flush marks the UoW rollback-only so the boundary cannot
            // commit partial state: a later commit is rejected.
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
        }

        [Fact]
        public async Task RollbackAsync_Cancelled_ShouldSurfaceCancellationAndStayNotCompleted()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource { ThrowIfCanceled = true };
            uow.RegisterResource(resource);
            await ((IUnitOfWorkInternal)uow).ActivatePendingResourcesAsync();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // An explicit rollback that is cancelled surfaces the cancellation; the UoW stays
            // not-completed so the caller can observe the uncertain outcome.
            var ex = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => uow.RollbackAsync(cts.Token));
            Assert.Contains(ex.RollbackExceptions, e => e is OperationCanceledException);
            Assert.False(uow.IsCompleted);
        }

        #endregion

        #region W3: Partially created savepoints must stay usable

        [Fact]
        public async Task CreateSavepointAsync_PartialResourceFailure_ShouldKeepCreatedSavepointsUsable()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource1 = new TestUowResource();
            var resource2 = new TestUowResource { CreateSavepointException = new InvalidOperationException("Savepoint failed") };
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);

            await Assert.ThrowsAsync<AggregateException>(() => uow.CreateSavepointAsync("test_sp"));

            // resource1's savepoint was created; the UoW must keep it usable instead of reporting it missing.
            Assert.Contains("test_sp", resource1.Savepoints);
            await uow.RollbackToSavepointAsync("test_sp");
            Assert.Contains("test_sp", resource1.RolledBackTo);
        }

        #endregion

        #region W4: Supplementary coverage - ambient isolation, ordering, async disposal

        [Fact]
        public async Task BeginAsync_ConcurrentAsyncBranches_ShouldIsolateAmbientFramesAndRestoreOuter()
        {
            var manager = CreateManager(out _);

            using var outer = await manager.BeginAsync();

            async Task BranchAsync(int id)
            {
                using var inner = await manager.BeginAsync();
                Assert.Same(inner, manager.Current);
                Assert.Same(outer, inner.Parent);
                await Task.Yield();
                Assert.Same(inner, manager.Current);
            }

            await Task.WhenAll(BranchAsync(1), BranchAsync(2));

            Assert.Same(outer, manager.Current);
        }

        [Fact]
        public async Task FlushAsync_ShouldFlushResourcesInExactRegistrationOrder()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var log = new List<string>();
            var resource1 = new TestUowResource { Name = "r1", SharedCallLog = log };
            var resource2 = new TestUowResource { Name = "r2", SharedCallLog = log };
            uow.RegisterResource(resource1);
            uow.RegisterResource(resource2);

            await uow.FlushAsync();

            Assert.Equal(new[]
            {
                "r1.Prepare",
                "r2.Prepare",
                "r1.EnsureTransaction",
                "r2.EnsureTransaction",
                "r1.Flush",
                "r2.Flush"
            }, log);
        }

        [Fact]
        public async Task DisposeAsync_ShouldUseAsyncDisposalPath()
        {
            var uow = new UnitOfWork(_logger, new UnitOfWorkOptions(), null);
            var resource = new TestUowResource();
            uow.RegisterResource(resource);
            await uow.CommitAsync();

            await uow.DisposeAsync();

            Assert.Equal(1, resource.AsyncDisposeCount);
        }

        #endregion
    }
}
