using MiCake.Core.DependencyInjection;
using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Internal
{
    /// <summary>
    /// Failure-path and post-save re-entry tests for the save-operation state machine:
    /// database failures, post-save handler failures, cancellation, post-save controlled
    /// re-entry, and real pooled-context behavior.
    /// </summary>
    public class SaveOperationReentryFailureTests : IDisposable
    {
        private readonly string _dbPath;

        public SaveOperationReentryFailureTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"micake-saveop-failure-{Guid.NewGuid():N}.db");
            PostSaveAddsOnceHandler.Reset();
            PostSaveNoChangeHandler.Reset();
            ThrowOncePostSaveHandler.Reset();
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        private ServiceProvider BuildProvider(Action<IServiceCollection>? registerHandlers = null)
        {
            var services = new ServiceCollection();
            services.AddDbContext<UniqueLifecycleTestDbContext>((sp, opt) =>
                opt.UseSqlite($"Data Source={_dbPath};Pooling=False"));
            services.ConfigureDbContext<UniqueLifecycleTestDbContext>((sp, builder) =>
            {
                builder.UseMiCake();
                builder.AddInterceptors(
                    sp.GetRequiredService<MiCakeEFCoreInterceptor>(),
                    sp.GetRequiredService<MiCakeDbCommandInterceptor>());
            });
            services.AddLogging();
            services.AddUowCoreServices(typeof(UniqueLifecycleTestDbContext));

            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(UniqueLifecycleTestDbContext)));
            services.AddSingleton<MiCakeEFCoreInterceptor>();
            services.AddSingleton<MiCakeDbCommandInterceptor>();

            registerHandlers?.Invoke(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IDbContextTypeRegistry>().RegisterDbContextType(typeof(UniqueLifecycleTestDbContext));
            return provider;
        }

        [Fact]
        public async Task PostSaveReentry_HandlerAddsOnceAndSaves_CompletesInBoundedCycles()
        {
            using var provider = BuildProvider(s =>
                s.AddScoped<IRepositoryPostSaveChanges, PostSaveAddsOnceHandler>());
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new UniqueLifecycleTestEntity { Name = "root" });
            await context.SaveChangesAsync();
            await uow.CommitAsync();

            // The post-save handler runs once per save cycle and its nested SaveChanges is
            // suppressed into a follow-up cycle; both entities are persisted and no
            // recursion or duplicate post-save side effects occur.
            Assert.Equal(2, PostSaveAddsOnceHandler.CallCount);
            Assert.Equal(2, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task PostSaveReentry_WithoutNewChanges_ThrowsNoProgress_AndCommitIsRejected()
        {
            using var provider = BuildProvider(s =>
                s.AddScoped<IRepositoryPostSaveChanges, PostSaveNoChangeHandler>());
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new UniqueLifecycleTestEntity { Name = "root" });

            var exception = await Assert.ThrowsAsync<SaveChangesReentryException>(() => context.SaveChangesAsync());
            Assert.Contains("no progress", exception.Message, StringComparison.OrdinalIgnoreCase);

            // The unit of work is rollback-only: commit is rejected and data is discarded.
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
            await uow.RollbackAsync();
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task DatabaseFailure_EndsOperation_RollsBack_AndNextSaveStartsFreshRoot()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new UniqueLifecycleTestEntity { Name = "dup" });
            context.Add(new UniqueLifecycleTestEntity { Name = "dup" }); // unique constraint violation

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            // The failed save may have written rows into the open transaction, so the unit
            // of work is rollback-only: commit is rejected and nothing becomes durable.
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
            await uow.RollbackAsync();
            Assert.Equal(0, await context.Entities.CountAsync());

            // The operation state was released; a fresh unit of work starts a clean root
            // operation on the same context after the caller detaches the failed entries.
            context.ChangeTracker.Clear();
            await using var uow2 = await uowManager.BeginAsync();
            context.Add(new UniqueLifecycleTestEntity { Name = "after" });
            await context.SaveChangesAsync();
            await uow2.CommitAsync();
            Assert.Equal(1, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task PostSaveFailure_MarksUnitOfWorkRollbackOnly_CommitRejected_AndNothingDurable()
        {
            using var provider = BuildProvider(s =>
                s.AddScoped<IRepositoryPostSaveChanges, ThrowOncePostSaveHandler>());
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new UniqueLifecycleTestEntity { Name = "first" });

            // First save fails in the post-save handler; the operation ends.
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

            // The data written by the failed save is already inside the uncommitted
            // transaction: the unit of work is rollback-only, so a later commit must be
            // rejected instead of making the handler failure silently durable.
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
            await uow.RollbackAsync();
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task Cancellation_FromPreSaveHandler_EndsOperation_AndRollbackWorks()
        {
            using var provider = BuildProvider(s =>
                s.AddScoped<IRepositoryPreSaveChanges, CancelPreSaveHandler>());
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new UniqueLifecycleTestEntity { Name = "cancel" });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.SaveChangesAsync());

            await uow.RollbackAsync();
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task PooledContext_SequentialScopes_DoNotLeakOperationState()
        {
            var services = new ServiceCollection();
            services.AddDbContextPool<UniqueLifecycleTestDbContext>((sp, opt) =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString())
                   .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            services.ConfigureDbContext<UniqueLifecycleTestDbContext>((sp, builder) =>
            {
                builder.UseMiCake();
                builder.AddInterceptors(
                    sp.GetRequiredService<MiCakeEFCoreInterceptor>(),
                    sp.GetRequiredService<MiCakeDbCommandInterceptor>());
            });
            services.AddLogging();
            services.AddUowCoreServices(typeof(UniqueLifecycleTestDbContext));

            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(UniqueLifecycleTestDbContext)));
            services.AddSingleton<MiCakeEFCoreInterceptor>();
            services.AddSingleton<MiCakeDbCommandInterceptor>();

            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IDbContextTypeRegistry>().RegisterDbContextType(typeof(UniqueLifecycleTestDbContext));

            // Each scope returns its pooled context, whose operation state must be reset
            // (IResettableService) before the next scope reuses it; a leaked root frame
            // would silently suppress the next scope's first save.
            for (int i = 0; i < 3; i++)
            {
                await using var scope = provider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await uowManager.BeginAsync();
                context.Add(new UniqueLifecycleTestEntity { Name = $"pooled-{i}" });
                await context.SaveChangesAsync();
                await uow.CommitAsync();
            }

            await using var verifyScope = provider.CreateAsyncScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();
            Assert.Equal(3, await verifyContext.Entities.CountAsync());
        }

        #region Test Handlers

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

                var context = _serviceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();

                // Add one entity and request another save from a post-save handler; the
                // nested save is suppressed and re-scanned into a follow-up cycle.
                context.Add(new UniqueLifecycleTestEntity { Name = $"post-{callCount}" });
                await context.SaveChangesAsync(cancellationToken);

                return entityState;
            }
        }

        public class PostSaveNoChangeHandler : IRepositoryPostSaveChanges
        {
            private readonly IServiceProvider _serviceProvider;
            public static int CallCount;
            public static void Reset() => CallCount = 0;

            public PostSaveNoChangeHandler(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public int Order { get; set; }

            public async ValueTask<RepositoryEntityStates> PostSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref CallCount);

                var context = _serviceProvider.GetRequiredService<UniqueLifecycleTestDbContext>();

                // Re-enter without making any tracker change: no progress is possible.
                await context.SaveChangesAsync(cancellationToken);

                return entityState;
            }
        }

        public class ThrowOncePostSaveHandler : IRepositoryPostSaveChanges
        {
            public static int CallCount;
            public static void Reset() => CallCount = 0;

            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PostSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
            {
                var callCount = Interlocked.Increment(ref CallCount);
                if (callCount == 1)
                {
                    throw new InvalidOperationException("post-save failure");
                }

                return new ValueTask<RepositoryEntityStates>(entityState);
            }
        }

        public class CancelPreSaveHandler : IRepositoryPreSaveChanges
        {
            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
                => throw new OperationCanceledException(cancellationToken);
        }

        #endregion

        public class UniqueLifecycleTestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class UniqueLifecycleTestDbContext : DbContext
        {
            public UniqueLifecycleTestDbContext(DbContextOptions<UniqueLifecycleTestDbContext> options) : base(options)
            {
            }

            public DbSet<UniqueLifecycleTestEntity> Entities => Set<UniqueLifecycleTestEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<UniqueLifecycleTestEntity>()
                    .HasIndex(e => e.Name)
                    .IsUnique();
            }
        }
    }
}
