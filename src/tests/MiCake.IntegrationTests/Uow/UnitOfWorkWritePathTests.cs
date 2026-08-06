using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.DDD.Uow;
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
    /// t7 relational acceptance matrix (part 2): first-write transaction binding for
    /// SaveChanges/bulk/raw SQL, generated-key flush, detached concurrency, lifecycle
    /// failure, post-save re-entry convergence, and host isolation on file-backed SQLite.
    /// </summary>
    public class UnitOfWorkWritePathTests : IDisposable
    {
        private readonly SqliteUnitOfWorkFixture _fixture = new();

        public UnitOfWorkWritePathTests()
        {
            ThrowingPreSaveHandler.Reset();
            PostSaveAddsOnceHandler.Reset();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        private static async Task EnsureCreatedAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
            await context.Database.EnsureCreatedAsync();

            // Pooled contexts return to the EF pool with the connection still open; close it
            // explicitly so SQLite releases the schema lock before the next scope leases the
            // same context instance.
            await context.Database.CloseConnectionAsync();
        }

        private static async Task SeedAsync(ServiceProvider provider, params string[] names)
        {
            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await manager.BeginAsync();
            var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
            foreach (var name in names)
            {
                context.Aggregates.Add(new UowAcceptanceAggregate(name));
            }

            await uow.CommitAsync();
        }

        private static async Task<int> CountAsync(ServiceProvider provider)
        {
            // Read verification runs outside any unit of work, so the DbContext is resolved
            // directly from DI (the frame-stable factory requires an ambient UoW).
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
            return await context.Aggregates.CountAsync();
        }

        [Fact]
        public async Task FirstSaveChanges_BindsToUoWTransaction_RollsBack()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("first-write"));
                await context.SaveChangesAsync();

                await uow.RollbackAsync();
            }

            Assert.Equal(0, await CountAsync(provider));
        }

        [Fact]
        public async Task ExecuteUpdate_RollsBackWithUoW()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);
            await SeedAsync(provider, "alpha", "beta");

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                await context.Aggregates
                    .Where(a => a.Name == "alpha")
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.Name, "renamed"));

                await uow.RollbackAsync();
            }

            await using (var verifyScope = provider.CreateAsyncScope())
            {
                var context = verifyScope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
                Assert.Equal(1, await context.Aggregates.CountAsync(a => a.Name == "alpha"));
            }
        }

        [Fact]
        public async Task ExecuteDelete_RollsBackWithUoW()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);
            await SeedAsync(provider, "alpha", "beta");

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                await context.Aggregates
                    .Where(a => a.Name == "alpha")
                    .ExecuteDeleteAsync();

                await uow.RollbackAsync();
            }

            Assert.Equal(2, await CountAsync(provider));
        }

        [Fact]
        public async Task ExecuteSqlRaw_RollsBackWithUoW()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);
            await SeedAsync(provider, "alpha", "beta");

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Aggregates\" WHERE \"Name\" = 'alpha'");

                await uow.RollbackAsync();
            }

            Assert.Equal(2, await CountAsync(provider));
        }

        [Fact]
        public async Task WriteWithoutUoW_IsRejected()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();

            context.Aggregates.Add(new UowAcceptanceAggregate("no-uow"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("unit of work", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(0, await CountAsync(provider));
        }

        [Fact]
        public async Task FlushAsync_AssignsGeneratedKey_RollsBackCleanly()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                var aggregate = new UowAcceptanceAggregate("generated-key");
                context.Aggregates.Add(aggregate);

                // The integer identity is database-generated; flush materializes it
                // without committing, and rollback discards the row.
                var rows = await uow.FlushAsync();
                Assert.Equal(1, rows);
                Assert.True(aggregate.Id > 0);

                await uow.RollbackAsync();
            }

            Assert.Equal(0, await CountAsync(provider));
        }

        [Fact]
        public async Task DetachedStaleReplacement_RaisesConcurrencyConflict()
        {
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);
            await SeedAsync(provider, "original");

            // Load the aggregate in one scope, modify it in a second scope (committed),
            // then replace it from the first scope's stale detached instance.
            UowAcceptanceAggregate stale;
            int id;
            await using (var readScope = provider.CreateAsyncScope())
            {
                var context = readScope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
                stale = await context.Aggregates.AsNoTracking().FirstAsync();
                id = stale.Id;
            }

            await using (var modifyScope = provider.CreateAsyncScope())
            {
                var manager = modifyScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(modifyScope.ServiceProvider);

                // The concurrent change is applied as a bulk update (as another session would):
                // it mutates both the row and the optimistic-concurrency token without loading
                // the aggregate, so the stale replacement later carries an outdated Version.
                await context.Aggregates
                    .Where(a => a.Id == id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.Name, "concurrently-changed")
                        .SetProperty(a => a.Version, "concurrent-version"));

                await uow.CommitAsync();
            }

            await using (var replaceScope = provider.CreateAsyncScope())
            {
                var manager = replaceScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var repository = replaceScope.ServiceProvider.GetRequiredService<IRepository<UowAcceptanceAggregate, int>>();

                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(replaceScope.ServiceProvider);
                stale.ChangeName("stale-replacement");
                await repository.UpdateAsync(stale);

                // The stale replacement carries the original concurrency token; the committed
                // concurrent change must surface as a concurrency conflict.
                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => uow.FlushAsync());
                await uow.RollbackAsync();
            }

            await using (var verifyScope = provider.CreateAsyncScope())
            {
                var context = verifyScope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
                var name = await context.Aggregates.Select(a => a.Name).SingleAsync();
                Assert.Equal("concurrently-changed", name);
            }
        }

        [Fact]
        public async Task LifecycleFailure_RollsBack_AndPersistsNothing()
        {
            using var provider = _fixture.BuildProvider(configure: s =>
                s.AddScoped<IRepositoryPreSaveChanges, ThrowingPreSaveHandler>());
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("lifecycle-failure"));

                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

                // The failed save leaves the unit of work rollback-only; commit is rejected.
                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
                await uow.RollbackAsync();
            }

            Assert.Equal(0, await CountAsync(provider));
        }

        [Fact]
        public async Task PostSaveReentry_Converges_AndCommitsAllWrites()
        {
            using var provider = _fixture.BuildProvider(configure: s =>
                s.AddScoped<IRepositoryPostSaveChanges, PostSaveAddsOnceHandler>());
            await EnsureCreatedAsync(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("root"));

                // The nested save inside the post-save handler is suppressed and converges
                // in a follow-up cycle; both entities commit atomically.
                await context.SaveChangesAsync();
                await uow.CommitAsync();
            }

            Assert.Equal(2, await CountAsync(provider));
        }

        [Fact]
        public async Task TwoHosts_IsolatedFrames_DoNotInterfere()
        {
            // Each host owns its fixture (and therefore its own database file) so the
            // assertions measure frame isolation, not shared-file interference.
            using var fixtureA = new SqliteUnitOfWorkFixture();
            using var fixtureB = new SqliteUnitOfWorkFixture();
            using var providerA = fixtureA.BuildProvider();
            using var providerB = fixtureB.BuildProvider();
            await EnsureCreatedAsync(providerA);
            await EnsureCreatedAsync(providerB);

            // Host A commits; host B rolls back. Neither may leak into the other.
            await using (var scopeA = providerA.CreateAsyncScope())
            {
                var manager = scopeA.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scopeA.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("host-a"));
                await uow.CommitAsync();
            }

            await using (var scopeB = providerB.CreateAsyncScope())
            {
                var manager = scopeB.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scopeB.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("host-b"));
                await uow.RollbackAsync();
            }

            Assert.Equal(1, await CountAsync(providerA));
            Assert.Equal(0, await CountAsync(providerB));
        }

        [Fact]
        public async Task TrackedUpdate_RolledBackByUoW_KeepsOriginalState()
        {
            // Supersedes the skipped InMemory "Transaction_Rollback_ShouldDiscardChanges"
            // repository test: the same guarantee proven through the MiCake UoW contract
            // on a relational store instead of a raw EF transaction on InMemory.
            using var provider = _fixture.BuildProvider();
            await EnsureCreatedAsync(provider);
            await SeedAsync(provider, "Rollback Test");

            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);

                var existing = await context.Aggregates.SingleAsync();
                existing.ChangeName("Should Be Rolled Back");
                await context.SaveChangesAsync();

                await uow.RollbackAsync();
            }

            await using (var verifyScope = provider.CreateAsyncScope())
            {
                var context = verifyScope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
                var name = await context.Aggregates.Select(a => a.Name).SingleAsync();
                Assert.Equal("Rollback Test", name);
            }
        }

        [Fact]
        public async Task PooledContext_CommitAndRollback_IsolatedAcrossPoolReuses()
        {
            // The primary DbContext is registered through AddDbContextPool; every pooled
            // instance must honor the UoW transaction contract and the pool-reset state
            // contract (no leaked frame tokens or operation state between reuses).
            using var provider = _fixture.BuildPooledProvider();
            await EnsureCreatedAsync(provider);

            await using (var commitScope = provider.CreateAsyncScope())
            {
                var manager = commitScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(commitScope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("pooled-commit"));
                await uow.CommitAsync();
            }

            await using (var rollbackScope = provider.CreateAsyncScope())
            {
                var manager = rollbackScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(rollbackScope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("pooled-rollback"));
                await uow.RollbackAsync();
            }

            await using (var secondCommitScope = provider.CreateAsyncScope())
            {
                var manager = secondCommitScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = SqliteUnitOfWorkFixture.GetPrimaryContext(secondCommitScope.ServiceProvider);
                context.Aggregates.Add(new UowAcceptanceAggregate("pooled-second-commit"));
                await uow.CommitAsync();
            }

            await using (var verifyScope = provider.CreateAsyncScope())
            {
                var context = verifyScope.ServiceProvider.GetRequiredService<UowAcceptanceDbContext>();
                var names = await context.Aggregates.Select(a => a.Name).OrderBy(n => n).ToListAsync();
                Assert.Equal(new[] { "pooled-commit", "pooled-second-commit" }, names);
            }
        }

        #region Test Handlers

        public class ThrowingPreSaveHandler : IRepositoryPreSaveChanges
        {
            public static int CallCount;

            public static void Reset() => CallCount = 0;

            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref CallCount);
                throw new InvalidOperationException("pre-save failure");
            }
        }

        public class PostSaveAddsOnceHandler : IRepositoryPostSaveChanges
        {
            private readonly IServiceProvider _serviceProvider;

            public static int CallCount;

            public static void Reset() => CallCount = 0;

            public PostSaveAddsOnceHandler(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public int Order { get; set; }

            public async ValueTask<RepositoryEntityStates> PostSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                var callCount = Interlocked.Increment(ref CallCount);
                if (callCount > 1)
                {
                    return entityState;
                }

                var context = _serviceProvider.GetRequiredService<UowAcceptanceDbContext>();

                // Add one entity and request another save from a post-save handler; the
                // nested save is suppressed and re-scanned into a follow-up cycle.
                context.Aggregates.Add(new UowAcceptanceAggregate($"post-{callCount}"));
                await context.SaveChangesAsync(cancellationToken);

                return entityState;
            }
        }

        #endregion
    }
}
