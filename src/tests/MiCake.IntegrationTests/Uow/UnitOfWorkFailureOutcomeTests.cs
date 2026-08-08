using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.DDD.Uow.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.IntegrationTests.Uow
{
    /// <summary>
    /// Relational acceptance matrix (part 3): multi-resource failure semantics on
    /// file-backed SQLite. The relational resource provides the durable backbone while a
    /// hand-written <see cref="IUnitOfWorkResource"/> double injects commit/rollback failures
    /// that a real provider cannot reproduce deterministically.
    /// </summary>
    public class UnitOfWorkFailureOutcomeTests : IDisposable
    {
        private readonly SqliteUnitOfWorkFixture _fixture = new();

        public void Dispose()
        {
            _fixture.Dispose();
        }

        private static async Task EnsureCreatedAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>().Database.EnsureCreatedAsync();
        }

        private static async Task<int> CountAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
            return await context.Aggregates.CountAsync();
        }

        [Fact]
        public async Task PartialCommit_FirstResourceDurable_OutcomeStructured_UoWTerminal()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            var failingResource = new FailingAcceptanceResource
            {
                ResourceType = "FailingSecondResource",
                CommitException = new InvalidOperationException("simulated second-resource commit failure")
            };

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var uow = await manager.BeginAsync();

                // The relational resource registers FIRST so it commits before the failing one;
                // its commit becomes durable and cannot be undone by the later failure.
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("durable-write"));

                ((IUnitOfWorkInternal)uow).RegisterResource(failingResource);

                var exception = await Assert.ThrowsAsync<PartialUnitOfWorkCommitException>(() => uow.CommitAsync());

                // Structured outcome: the durable resource is Committed, the failing one Failed;
                // CommitState and RollbackState are tracked separately: a commit-failed resource
                // is never conflated with a resource that was never committed.
                var durableOutcome = Assert.Single(exception.Outcome.Resources,
                    r => r.CommitState == UnitOfWorkResourceCommitState.Committed);
                Assert.Contains("UowAcceptanceDbContext", durableOutcome.ResourceType, StringComparison.Ordinal);

                var failedOutcome = Assert.Single(exception.Outcome.Resources,
                    r => r.CommitState == UnitOfWorkResourceCommitState.Failed);
                Assert.Equal("FailingSecondResource", failedOutcome.ResourceType);
                Assert.Equal(UnitOfWorkResourceCommitState.RolledBack, failedOutcome.RollbackState);

                Assert.Single(exception.CommitFailures);
                Assert.Empty(exception.RollbackFailures);

                // Partial commit is a terminal state: the UoW cannot be rolled back as a whole;
                // only the boundary may dispose it.
                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackAsync());
                await uow.DisposeAsync();
            }

            // Best-effort semantics: the committed relational write remains durable.
            Assert.Equal(1, await CountAsync(provider));
        }

        [Fact]
        public async Task RollbackFailure_BoundaryExceptionRaised_AndRelationalRollbackStillHolds()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            var failingResource = new FailingAcceptanceResource
            {
                ResourceType = "FailingRollbackResource",
                RollbackException = new InvalidOperationException("simulated rollback failure")
            };

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var uow = await manager.BeginAsync();

                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("will-rollback"));

                ((IUnitOfWorkInternal)uow).RegisterResource(failingResource);

                // The explicit rollback fails for one resource; the boundary exception must
                // carry every rollback failure without losing the relational rollback result.
                var exception = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => uow.RollbackAsync());
                Assert.Contains(exception.RollbackExceptions,
                    e => e.Message.Contains("simulated rollback failure", StringComparison.Ordinal));

                // The failing resource no longer holds an active transaction from the test's
                // perspective, so disposal completes without retrying the failed rollback.
                failingResource.SimulateProviderTransactionLost();
                await uow.DisposeAsync();
            }

            // The relational resource rolled back successfully despite the other resource's failure.
            Assert.Equal(0, await CountAsync(provider));
            Assert.Equal(1, failingResource.RollbackCount);
        }

        /// <summary>
        /// Minimal <see cref="IUnitOfWorkResource"/> double with injectable commit/rollback
        /// failures, modeled on the framework's TestUowResource. Provider-level commit and
        /// rollback failures cannot be forced deterministically on a live SQLite connection,
        /// so the double stands in for the failing second resource at the UoW boundary.
        /// </summary>
        private sealed class FailingAcceptanceResource : IUnitOfWorkResource
        {
            public UnitOfWorkResourceId Id { get; } = new(Guid.NewGuid());
            public string ResourceType { get; init; } = "FailingAcceptanceResource";
            public bool HasActiveTransaction { get; private set; }
            public bool SupportsSavepoints => true;

            public Exception? CommitException { get; init; }
            public Exception? RollbackException { get; init; }

            public int CommitCount { get; private set; }
            public int RollbackCount { get; private set; }

            public void Prepare(UnitOfWorkResourceContext context)
            {
                // No I/O: preparation only records the owning unit of work.
            }

            public void EnsureTransaction() => HasActiveTransaction = true;

            public ValueTask EnsureTransactionAsync(CancellationToken cancellationToken = default)
            {
                HasActiveTransaction = true;
                return ValueTask.CompletedTask;
            }

            public ValueTask<int> FlushAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(0);

            public ValueTask CommitAsync(CancellationToken cancellationToken = default)
            {
                CommitCount++;
                if (CommitException != null)
                {
                    throw CommitException;
                }

                HasActiveTransaction = false;
                return ValueTask.CompletedTask;
            }

            public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
            {
                RollbackCount++;
                if (RollbackException != null)
                {
                    throw RollbackException;
                }

                HasActiveTransaction = false;
                return ValueTask.CompletedTask;
            }

            public ValueTask CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
                => ValueTask.CompletedTask;

            public ValueTask RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
                => ValueTask.CompletedTask;

            public ValueTask ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
                => ValueTask.CompletedTask;

            /// <summary>
            /// Models a provider whose connection was lost after a failed rollback, so the
            /// UoW disposal path does not retry the injected failure.
            /// </summary>
            public void SimulateProviderTransactionLost() => HasActiveTransaction = false;

            public void Dispose() => HasActiveTransaction = false;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
