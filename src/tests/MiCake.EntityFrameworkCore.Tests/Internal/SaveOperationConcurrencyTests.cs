using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Internal
{
    /// <summary>
    /// Concurrency and lifecycle state-machine tests for the per-DbContext save-operation
    /// accessor: operation isolation, pool reset, failure cleanup, pre-save
    /// state retention, controlled re-entry, and the bounded save-cycle limit.
    /// </summary>
    [Collection("SaveOperation")]
    public class SaveOperationConcurrencyTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly Xunit.Abstractions.ITestOutputHelper _out;

        public SaveOperationConcurrencyTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            _out = output;
            _dbPath = Path.Combine(Path.GetTempPath(), $"micake-saveop-{Guid.NewGuid():N}.db");
            StateRecordingPreSaveHandler.Reset();
            StateRecordingPostSaveHandler.Reset();
            ResetInPreSaveHandler.Reset();
            ThrowOncePreSaveHandler.Reset();
            NestedSavePreSaveHandler.Reset();
            EventCollectingPreSaveHandler.Reset();
            UnboundedChangePostSaveHandler.Reset();
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        private ServiceProvider BuildProvider(
            int maxSaveCycles = 16,
            Action<IServiceCollection>? registerHandlers = null,
            params Type[] extraInterceptorTypes)
        {
            var services = new ServiceCollection();
            services.AddDbContext<LifecycleTestDbContext>((sp, opt) =>
                opt.UseSqlite($"Data Source={_dbPath};Pooling=False"));
            services.ConfigureDbContext<LifecycleTestDbContext>((sp, builder) =>
            {
                builder.UseMiCake();
                builder.AddInterceptors(
                    sp.GetRequiredService<MiCakeEFCoreInterceptor>(),
                    sp.GetRequiredService<MiCakeDbCommandInterceptor>());
                foreach (var type in extraInterceptorTypes)
                {
                    builder.AddInterceptors((IInterceptor)sp.GetRequiredService(type));
                }
            });
            services.AddLogging();
            services.AddUowCoreServices(typeof(LifecycleTestDbContext));

            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(
                new MiCakeEFCoreOptions(typeof(LifecycleTestDbContext)) { MaxSaveCycles = maxSaveCycles });
            services.AddSingleton<MiCakeEFCoreInterceptor>();
            services.AddSingleton<MiCakeDbCommandInterceptor>();

            registerHandlers?.Invoke(services);

            foreach (var type in extraInterceptorTypes)
            {
                services.AddSingleton(type);
            }

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IDbContextTypeRegistry>().RegisterDbContextType(typeof(LifecycleTestDbContext));
            return provider;
        }

        [Fact]
        public async Task PostSaveHandler_ReceivesPreSaveState_NotLiveEntryState()
        {
            using var provider = BuildProvider(registerHandlers: s =>
            {
                s.AddScoped<IRepositoryPreSaveChanges, StateRecordingPreSaveHandler>();
                s.AddScoped<IRepositoryPostSaveChanges, StateRecordingPostSaveHandler>();
            });
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new LifecycleTestEntity { Name = "one" });
            await context.SaveChangesAsync();
            await uow.CommitAsync();

            // The post-save handler received the pre-save Added state even though EF
            // changed the live entry state to Unchanged after persistence.
            Assert.Single(StateRecordingPostSaveHandler.States);
            Assert.Equal(RepositoryEntityStates.Added, StateRecordingPostSaveHandler.States[0]);
            Assert.Equal(EntityState.Unchanged, context.Entry(await context.Entities.SingleAsync()).State);
        }

        [Fact]
        public async Task ConcurrentScopes_DoNotShareOperationState()
        {
            using var provider = BuildProvider(registerHandlers: s =>
            {
                s.AddScoped<IRepositoryPreSaveChanges, StateRecordingPreSaveHandler>();
                s.AddScoped<IRepositoryPostSaveChanges, StateRecordingPostSaveHandler>();
            });
            await using (var setupScope = provider.CreateAsyncScope())
            {
                await setupScope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>().Database.EnsureCreatedAsync();
            }

            var taskA = Task.Run(async () =>
            {
                await using var scope = provider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await uowManager.BeginAsync();
                context.Add(new LifecycleTestEntity { Name = "A" });
                await context.SaveChangesAsync();
                await uow.CommitAsync();
            });

            var taskB = Task.Run(async () =>
            {
                await using var scope = provider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await uowManager.BeginAsync();
                context.Add(new LifecycleTestEntity { Name = "B" });
                await context.SaveChangesAsync();
                await uow.CommitAsync();
            });

            await Task.WhenAll(taskA, taskB);

            await using var verifyScope = provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            Assert.Equal(2, await verifyContext.Entities.CountAsync());
            Assert.Equal(2, StateRecordingPostSaveHandler.EntityNames.Count);
        }

        [Fact]
        public async Task PoolReset_ClearsActiveOperation()
        {
            using var provider = BuildProvider(registerHandlers: s =>
            {
                s.AddScoped<IRepositoryPreSaveChanges, ResetInPreSaveHandler>();
                s.AddScoped<IRepositoryPostSaveChanges, StateRecordingPostSaveHandler>();
            });
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new LifecycleTestEntity { Name = "reset" });

            // The pre-save handler resets the accessor, simulating a pooled context being
            // returned to the pool mid-operation; the post-save handlers must not run.
            await context.SaveChangesAsync();
            await uow.RollbackAsync();

            Assert.Empty(StateRecordingPostSaveHandler.EntityNames);
            Assert.True(ResetInPreSaveHandler.Invoked);
        }

        [Fact]
        public async Task PreSaveFailure_MarksUnitOfWorkRollbackOnly_CommitRejected_AndNothingDurable()
        {
            using var provider = BuildProvider(registerHandlers: s =>
            {
                s.AddScoped<IRepositoryPreSaveChanges, ThrowOncePreSaveHandler>();
            });
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new LifecycleTestEntity { Name = "first" });

            // First save fails in the pre-save handler.
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

            // The pre-save failure leaves the unit of work rollback-only: the transaction
            // is already activated and the handler pass may have mutated tracker state, so
            // a later commit must be rejected instead of persisting that state.
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
            await uow.RollbackAsync();
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task NestedSaveChanges_IsSuppressedAndReScannedIntoFollowUpCycle()
        {
            using var provider = BuildProvider(registerHandlers: s =>
            {
                s.AddScoped<IRepositoryPreSaveChanges, NestedSavePreSaveHandler>();
            });
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new LifecycleTestEntity { Name = "root" });
            await context.SaveChangesAsync();
            await uow.CommitAsync();

            // The nested SaveChanges from the handler was suppressed and its pending entity
            // was absorbed by the enclosing save; both rows are persisted without recursion
            // or a spurious no-progress failure.
            Assert.True(NestedSavePreSaveHandler.CallCount >= 1);
            Assert.Equal(2, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task DomainEvents_AreNotDispatchedTwiceAcrossCycles()
        {
            using var provider = BuildProvider(registerHandlers: s =>
            {
                s.AddScoped<IRepositoryPreSaveChanges, EventCollectingPreSaveHandler>();
            });
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            var entity = new LifecycleTestEntity { Name = "events" };
            entity.AddTestEvent();
            context.Add(entity);
            await context.SaveChangesAsync();
            await uow.CommitAsync();

            // The event was collected exactly once even though the handler triggered a
            // follow-up save cycle (re-entry).
            Assert.Equal(1, EventCollectingPreSaveHandler.TotalEventsCollected);
        }

        [Fact]
        public async Task Cancellation_DuringSqlPhase_EndsOperation_MarksRollbackOnly_AndContextIsReusable()
        {
            using var cts = new CancellationTokenSource();
            using var provider = BuildProvider(
                registerHandlers: s => s.AddSingleton(cts),
                extraInterceptorTypes: typeof(CancelAfterSavingChangesInterceptor));
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            // The MiCake interceptor completes SavingChanges (root frame established), then
            // the test interceptor cancels the token: EF routes the resulting OCE to
            // SaveChangesCanceled, which must end the operation and mark the UoW rollback-only.
            await using (var uow = await uowManager.BeginAsync())
            {
                context.Add(new LifecycleTestEntity { Name = "canceled" });
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.SaveChangesAsync(cts.Token));

                // Partial rows may already be inside the open transaction: commit must be rejected.
                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
                await uow.RollbackAsync();
            }

            Assert.Equal(0, await context.Entities.CountAsync());

            // The frame was cleared by SaveChangesCanceled: the same context must save again
            // normally instead of being silently suppressed into a no-op.
            context.ChangeTracker.Clear();
            await using (var uow2 = await uowManager.BeginAsync())
            {
                context.Add(new LifecycleTestEntity { Name = "after-cancel" });
                var saved = await context.SaveChangesAsync();
                Assert.Equal(1, saved);
                await uow2.CommitAsync();
            }

            Assert.Equal(1, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task MaxSaveCycles_WhenHandlerGeneratesChangesForever_ShouldThrow()
        {
            using var provider = BuildProvider(
                maxSaveCycles: 3,
                registerHandlers: s =>
                {
                    s.AddScoped<IRepositoryPostSaveChanges, UnboundedChangePostSaveHandler>();
                });
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new LifecycleTestEntity { Name = "seed" });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<SaveChangesReentryException>(() => context.SaveChangesAsync());

            Assert.Contains("maximum of 3 save cycles", exception.Message);
            Assert.True(UnboundedChangePostSaveHandler.CallCount >= 3);

            // The unit of work is rollback-only: commit is rejected and the data is discarded.
            var commitException = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
            Assert.Contains("rollback-only", commitException.Message, StringComparison.OrdinalIgnoreCase);

            await uow.RollbackAsync();
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task NoProgressReentry_Throws_AndCommitIsRejected()
        {
            using var provider = BuildProvider(registerHandlers: s =>
            {
                s.AddScoped<IRepositoryPostSaveChanges, NoChangeReentryPostSaveHandler>();
            });
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new LifecycleTestEntity { Name = "root" });

            // A post-save handler that calls SaveChanges without modifying anything makes
            // progress impossible; the save throws and the unit of work becomes rollback-only.
            var exception = await Assert.ThrowsAsync<SaveChangesReentryException>(() => context.SaveChangesAsync());
            Assert.Contains("no progress", exception.Message, StringComparison.OrdinalIgnoreCase);

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
            await uow.RollbackAsync();
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        #region Test Handlers

        public class StateRecordingPreSaveHandler : IRepositoryPreSaveChanges
        {
            public static readonly List<string> EntityNames = [];
            public static void Reset() => EntityNames.Clear();

            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                lock (EntityNames)
                {
                    EntityNames.Add(entity.GetType().Name);
                }

                return new ValueTask<RepositoryEntityStates>(entityState);
            }
        }

        public class StateRecordingPostSaveHandler : IRepositoryPostSaveChanges
        {
            public static readonly List<RepositoryEntityStates> States = [];
            public static readonly List<string> EntityNames = [];
            public static void Reset()
            {
                lock (States)
                {
                    States.Clear();
                    EntityNames.Clear();
                }
            }

            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PostSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                lock (States)
                {
                    States.Add(entityState);
                    EntityNames.Add(entity.GetType().Name);
                }

                return new ValueTask<RepositoryEntityStates>(entityState);
            }
        }

        public class ResetInPreSaveHandler : IRepositoryPreSaveChanges
        {
            private readonly IServiceProvider _serviceProvider;
            public static bool Invoked;
            public static void Reset() => Invoked = false;

            public ResetInPreSaveHandler(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                Invoked = true;

                var context = _serviceProvider.GetRequiredService<LifecycleTestDbContext>();
                var accessor = context.GetInfrastructure().GetService<SaveOperationStateAccessor>();
                Assert.NotNull(accessor);
                accessor!.ResetState();

                return new ValueTask<RepositoryEntityStates>(entityState);
            }
        }

        public class ThrowOncePreSaveHandler : IRepositoryPreSaveChanges
        {
            public static int CallCount;
            public static void Reset() => CallCount = 0;

            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref CallCount);
                if (CallCount == 1)
                {
                    throw new InvalidOperationException("pre-save failure");
                }

                return new ValueTask<RepositoryEntityStates>(entityState);
            }
        }

        /// <summary>
        /// Cancels the operation token after the MiCake interceptor has completed
        /// SavingChanges, so the cancellation surfaces from the database phase and EF routes
        /// it to SaveChangesCanceled instead of an interceptor-internal catch.
        /// </summary>
        public class CancelAfterSavingChangesInterceptor : ISaveChangesInterceptor
        {
            private readonly CancellationTokenSource _cts;

            public CancelAfterSavingChangesInterceptor(CancellationTokenSource cts)
            {
                _cts = cts;
            }

            public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            {
                _cts.Cancel();
                return result;
            }

            public ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                _cts.Cancel();
                return new(result);
            }
        }

        public class NestedSavePreSaveHandler : IRepositoryPreSaveChanges
        {
            private readonly IServiceProvider _serviceProvider;
            public static int CallCount;
            public static void Reset() => CallCount = 0;

            public NestedSavePreSaveHandler(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public int Order { get; set; }

            public async ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                var callCount = Interlocked.Increment(ref CallCount);

                if (callCount > 1)
                {
                    return entityState;
                }

                var context = _serviceProvider.GetRequiredService<LifecycleTestDbContext>();

                // Nested SaveChanges from a lifecycle handler is suppressed and recorded
                // as a re-entry request; the root operation re-scans afterwards.
                await context.SaveChangesAsync(cancellationToken);

                // Simulate new work generated by the handler that the follow-up cycle must save.
                context.Add(new LifecycleTestEntity { Name = $"handler-{callCount}" });

                return entityState;
            }
        }

        public class EventCollectingPreSaveHandler : IRepositoryPreSaveChanges
        {
            private readonly IServiceProvider _serviceProvider;
            public static int TotalEventsCollected;
            public static int CallCount;
            public static void Reset()
            {
                TotalEventsCollected = 0;
                CallCount = 0;
            }

            public EventCollectingPreSaveHandler(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public int Order { get; set; }

            public async ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                if (entity is IEntity domainEntity && domainEntity.DomainEvents.Count > 0)
                {
                    Interlocked.Add(ref TotalEventsCollected, domainEntity.DomainEvents.Count);
                    domainEntity.ClearDomainEvents();
                }

                var callCount = Interlocked.Increment(ref CallCount);
                if (callCount > 1)
                {
                    return entityState;
                }

                var context = _serviceProvider.GetRequiredService<LifecycleTestDbContext>();
                await context.SaveChangesAsync(cancellationToken);

                return entityState;
            }
        }

        public class NoChangeReentryPostSaveHandler : IRepositoryPostSaveChanges
        {
            private readonly IServiceProvider _serviceProvider;

            public NoChangeReentryPostSaveHandler(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public int Order { get; set; }

            public async ValueTask<RepositoryEntityStates> PostSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                var context = _serviceProvider.GetRequiredService<LifecycleTestDbContext>();

                // Nested SaveChanges after the save completed without any tracker change:
                // suppressed, but no progress is possible, so the operation must fail
                // deterministically.
                await context.SaveChangesAsync(cancellationToken);
                return entityState;
            }
        }

        public class UnboundedChangePostSaveHandler : IRepositoryPostSaveChanges
        {
            private readonly IServiceProvider _serviceProvider;
            public static int CallCount;
            public static void Reset() => CallCount = 0;

            public UnboundedChangePostSaveHandler(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public int Order { get; set; }

            public async ValueTask<RepositoryEntityStates> PostSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref CallCount);

                var context = _serviceProvider.GetRequiredService<LifecycleTestDbContext>();

                // Generate a new change after every save so the operation can never quiesce.
                context.Add(new LifecycleTestEntity { Name = $"unbounded-{CallCount}" });
                await context.SaveChangesAsync(cancellationToken);

                return entityState;
            }
        }

        #endregion

        public class LifecycleTestEntity : Entity<int>
        {
            public string Name { get; set; } = string.Empty;

            public void AddTestEvent() => RaiseDomainEvent(new TestDomainEvent());
        }

        public class TestDomainEvent : DomainEvent
        {
        }

        public class LifecycleTestDbContext : DbContext
        {
            public LifecycleTestDbContext(DbContextOptions<LifecycleTestDbContext> options) : base(options)
            {
            }

            public DbSet<LifecycleTestEntity> Entities => Set<LifecycleTestEntity>();
        }
    }
}
