using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.IntegrationTests.Uow
{
    /// <summary>
    /// Relational acceptance matrix (part 1): root/shared nested/requiresNew/read-only/
    /// savepoint composition against file-backed SQLite.
    /// </summary>
    public class UnitOfWorkCompositionMatrixTests : IDisposable
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

        private static async Task<int> CountAggregatesAsync(ServiceProvider provider)
        {
            // Read verification runs outside any unit of work, so the DbContext is resolved
            // directly from DI (the frame-stable factory requires an ambient UoW).
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
            return await context.Aggregates.CountAsync();
        }

        [Fact]
        public async Task RootCommit_PersistsAcrossScopes()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);

                context.Aggregates.Add(new UowAcceptanceAggregate("committed"));
                await uow.CommitAsync();
            }

            Assert.Equal(1, await CountAggregatesAsync(provider));
        }

        [Fact]
        public async Task RootRollback_LeavesNoTrace()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("rolled-back"));
                await uow.RollbackAsync();
            }

            Assert.Equal(0, await CountAggregatesAsync(provider));
        }

        [Fact]
        public async Task SharedNested_InnerCommit_DoesNotPhysicallyCommit()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var root = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                await using (var nested = await manager.BeginAsync())
                {
                    Assert.NotNull(nested.Parent);
                    context.Aggregates.Add(new UowAcceptanceAggregate("nested"));
                    await nested.CommitAsync(); // Shared nested completion is a no-op at the transaction level
                }

                // The nested write must not be visible before the root commits.
                Assert.Equal(0, await CountAggregatesAsync(provider));

                await root.CommitAsync();
            }

            Assert.Equal(1, await CountAggregatesAsync(provider));
        }

        [Fact]
        public async Task SharedNested_InnerRollback_MarksRootRollbackOnly()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var root = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("root-pending"));
                await using (var nested = await manager.BeginAsync())
                {
                    await nested.RollbackAsync(); // Nested rollback marks the root rollback-only
                }

                // A rollback-only root refuses to commit.
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => root.CommitAsync());
                Assert.Contains("rollback-only", exception.Message, StringComparison.OrdinalIgnoreCase);

                await root.RollbackAsync();
            }

            Assert.Equal(0, await CountAggregatesAsync(provider));
        }

        [Fact]
        public async Task RequiresNew_InnerCommit_LeavesOuterPending_AndRestoresAmbient()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var outer = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("outer-pending"));

                await manager.ExecuteRequiresNewAsync(async (isolatedProvider, ct) =>
                {
                    var innerContext = SqliteUnitOfWorkFixture.GetPrimaryContext(isolatedProvider);
                    innerContext.Aggregates.Add(new UowAcceptanceAggregate("inner-committed"));
                });

                // The inner requiresNew unit of work committed independently; the outer write
                // is still pending and must not be visible.
                Assert.Equal(1, await CountAggregatesAsync(provider));

                // The ambient unit of work was restored to the outer one.
                Assert.Same(outer, manager.Current);

                await outer.CommitAsync();
            }

            Assert.Equal(2, await CountAggregatesAsync(provider));
        }

        [Fact]
        public async Task RequiresNew_WithoutOuterUoW_Throws()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                manager.ExecuteRequiresNewAsync((_, ct) => Task.CompletedTask));
        }

        [Fact]
        public async Task RequiresNew_InnerFailure_RollsBackInner_LeavesOuterPending_AndRestoresAmbient()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var outer = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("outer-pending"));

                // The inner callback writes and then fails. The inner unit of work must roll
                // back its own flushed write, the outer pending state must remain untouched,
                // and the ambient frame must be restored to the outer unit of work.
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    manager.ExecuteRequiresNewAsync(async (isolatedProvider, ct) =>
                    {
                        var innerContext = SqliteUnitOfWorkFixture.GetPrimaryContext(isolatedProvider);
                        innerContext.Aggregates.Add(new UowAcceptanceAggregate("inner-doomed"));
                        await innerContext.SaveChangesAsync(ct);
                        throw new InvalidOperationException("inner failure");
                    }));

                // The inner write was rolled back and the outer write is still pending:
                // nothing may be visible yet.
                Assert.Equal(0, await CountAggregatesAsync(provider));

                // The ambient unit of work was restored to the outer one.
                Assert.Same(outer, manager.Current);

                await outer.CommitAsync();
            }

            // Only the outer write persisted; the rolled-back inner write left no trace.
            await using (var verifyScope = provider.CreateAsyncScope())
            {
                var context = verifyScope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
                var names = await context.Aggregates.Select(a => a.Name).ToListAsync();
                Assert.Equal(new[] { "outer-pending" }, names);
            }
        }

        [Fact]
        public async Task ReadOnly_RejectsFlush()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            await using var uow = await manager.BeginAsync(UnitOfWorkOptions.ReadOnly);
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.FlushAsync());
        }

        [Fact]
        public async Task ReadOnly_RejectsWrite_AndPersistsNothing()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await manager.BeginAsync(UnitOfWorkOptions.ReadOnly);
            var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
            context.Aggregates.Add(new UowAcceptanceAggregate("never-written"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("read-only", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(0, await CountAggregatesAsync(provider));
        }

        [Fact]
        public async Task Savepoint_RollsBackPartialWork_AndRejectsLateRegistration()
        {
            using var provider = _fixture.BuildProvider(registerSecondaryContext: true);
            await EnsureCreatedAsync(provider);

            await using (var setupScope = provider.CreateAsyncScope())
            {
                await setupScope.ServiceProvider.GetRequiredService<UowSecondaryDbContext>().Database.EnsureCreatedAsync();
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

                await using var uow = await manager.BeginAsync();

                // First write activates the primary resource transaction before the savepoint.
                var primaryContext = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                primaryContext.Aggregates.Add(new UowAcceptanceAggregate("before-savepoint"));
                await uow.FlushAsync();

                await uow.CreateSavepointAsync("checkpoint");

                // Work after the savepoint is rolled back to the checkpoint.
                primaryContext.Aggregates.Add(new UowAcceptanceAggregate("after-savepoint"));
                await uow.FlushAsync();

                await uow.RollbackToSavepointAsync("checkpoint");

                // The secondary resource registers only after the savepoint was created; using
                // the savepoint with it must fail with the unsupported-composition diagnostic.
                var secondaryContext = SqliteUnitOfWorkFixture.GetSecondaryContext(scope.ServiceProvider);
                secondaryContext.Entities.Add(new UowSecondaryEntity { Name = "late" });
                await secondaryContext.SaveChangesAsync();

                var lateException = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => uow.RollbackToSavepointAsync("checkpoint"));
                Assert.Contains("does not cover resource", lateException.Message, StringComparison.OrdinalIgnoreCase);

                // Final commit persists only the pre-savepoint primary write.
                await uow.CommitAsync();
            }

            Assert.Equal(1, await CountAggregatesAsync(provider));
            await using (var verifyScope = provider.CreateAsyncScope())
            {
                var secondaryContext = verifyScope.ServiceProvider.GetRequiredService<UowSecondaryDbContext>();
                Assert.Equal(1, await secondaryContext.Entities.CountAsync());
            }
        }

        [Fact]
        public async Task Savepoint_WithoutActiveTransaction_Throws()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            await using var uow = await manager.BeginAsync();

            // No resource has been touched yet, so no transaction is active.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CreateSavepointAsync("sp"));
            Assert.Contains("no active transaction", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Savepoint_AllResourcesActiveBeforeCreation_RollsBackAcrossAll()
        {
            using var provider = _fixture.BuildProvider(registerSecondaryContext: true);
            await EnsureCreatedAsync(provider);

            await using (var setupScope = provider.CreateAsyncScope())
            {
                await setupScope.ServiceProvider.GetRequiredService<UowSecondaryDbContext>().Database.EnsureCreatedAsync();
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();

                // Both resources are activated BEFORE the savepoint exists, so the savepoint
                // must cover both and roll back post-savepoint work on both.
                var primaryContext = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                var secondaryContext = SqliteUnitOfWorkFixture.GetSecondaryContext(scope.ServiceProvider);
                primaryContext.Aggregates.Add(new UowAcceptanceAggregate("primary-before"));
                secondaryContext.Entities.Add(new UowSecondaryEntity { Name = "secondary-before" });
                await uow.FlushAsync();

                await uow.CreateSavepointAsync("multi-checkpoint");

                primaryContext.Aggregates.Add(new UowAcceptanceAggregate("primary-after"));
                secondaryContext.Entities.Add(new UowSecondaryEntity { Name = "secondary-after" });
                await uow.FlushAsync();

                await uow.RollbackToSavepointAsync("multi-checkpoint");
                await uow.CommitAsync();
            }

            await using (var verifyScope = provider.CreateAsyncScope())
            {
                var primaryContext = verifyScope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
                var primaryNames = await primaryContext.Aggregates.Select(a => a.Name).ToListAsync();
                Assert.Equal(new[] { "primary-before" }, primaryNames);

                var secondaryContext = verifyScope.ServiceProvider.GetRequiredService<UowSecondaryDbContext>();
                var secondaryNames = await secondaryContext.Entities.Select(e => e.Name).ToListAsync();
                Assert.Equal(new[] { "secondary-before" }, secondaryNames);
            }
        }

        [Fact]
        public async Task ImmediateMode_ActivatesTransactions_OnBegin()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync(UnitOfWorkOptions.Immediate);

                // Immediate initialization resolves and activates every registered DbContext
                // resource when the unit of work begins, before any application write.
                Assert.True(uow.HasActiveTransactions);

                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("immediate"));
                await uow.CommitAsync();
            }

            Assert.Equal(1, await CountAggregatesAsync(provider));
        }

        [Fact]
        public async Task LazyMode_DefersTransactionActivation_UntilFirstWrite()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();

                // Lazy initialization defers transaction activation until the first write.
                Assert.False(uow.HasActiveTransactions);

                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("lazy"));
                await context.SaveChangesAsync();

                Assert.True(uow.HasActiveTransactions);
                await uow.CommitAsync();
            }

            Assert.Equal(1, await CountAggregatesAsync(provider));
        }
    }
}
