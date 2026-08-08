using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Internal
{
    /// <summary>
    /// Prototype validation for automatic interceptor installation through the EF Core 9+
    /// <c>ConfigureDbContext</c> mechanism. The tests use a minimal marker interceptor and
    /// do NOT depend on the MiCake write pipeline, isolating the framework mechanism from
    /// MiCake internals.
    ///
    /// The tests share static counters on the marker interceptors, so they must run serially
    /// (a shared xUnit collection disables intra-class parallelization).
    ///
    /// Scenarios under test:
    ///   1. ConfigureDbContext + AddDbContext -> interceptor is attached (no user call).
    ///   2. ConfigureDbContext + AddDbContextPool -> interceptor attached; pool-reset contract.
    ///   3. Same interceptor instance reused (no ManyServiceProvidersCreatedWarning).
    ///   4. ConfigureDbContext + user AddDbContext delegate compose in order.
    ///   5. Configurator-installed and user-installed interceptors compose (additive).
    ///   6. (Permissive policy is a MiCake-interceptor behavior; its guard logic is covered by
    ///      existing write-guard tests once the interceptor ctor is rewired — this prototype
    ///      only proves the attachment mechanism.)
    /// </summary>
    /// <summary>
    /// Disables parallelization for the prototype tests: they share static counters on
    /// the marker interceptors, which would race under xUnit's default intra-class
    /// parallel execution.
    /// </summary>
    [CollectionDefinition("ConfigureDbContextPrototype", DisableParallelization = true)]
    public class ConfigureDbContextPrototypeCollection
    {
    }

    [Collection("ConfigureDbContextPrototype")]
    public class ConfigureDbContextPrototypeTests
    {
        private sealed class PrototypeDbContext : DbContext
        {
            public PrototypeDbContext(DbContextOptions<PrototypeDbContext> options) : base(options)
            {
            }

            public DbSet<PrototypeEntity> Entities => Set<PrototypeEntity>();
        }

        private sealed class PrototypeEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// Marker interceptor: records that it was attached and invoked, and proves
        /// constructor injection from the application container works. Overrides the
        /// async save hooks because SaveChangesAsync drives the async interceptor
        /// pipeline (the sync SavingChanges override is never called for async saves).
        /// </summary>
        private sealed class MarkerInterceptor : SaveChangesInterceptor
        {
            public static int InstanceCount;
            public static int SaveCount;

            public MarkerInterceptor()
            {
                System.Threading.Interlocked.Increment(ref InstanceCount);
            }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                System.Threading.Interlocked.Increment(ref SaveCount);
                return new ValueTask<InterceptionResult<int>>(result);
            }
        }

        /// <summary>
        /// Interceptor installed by the user inside AddDbContext; proves composition with
        /// the configurator-installed marker interceptor.
        /// </summary>
        private sealed class UserInterceptor : SaveChangesInterceptor
        {
            public static int InstanceCount;
            public static int SaveCount;

            public UserInterceptor()
            {
                System.Threading.Interlocked.Increment(ref InstanceCount);
            }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                System.Threading.Interlocked.Increment(ref SaveCount);
                return new ValueTask<InterceptionResult<int>>(result);
            }
        }

        [Fact]
        public async Task Gate1_ConfigureDbContext_AttachesInterceptor_WithoutUserCall()
        {
            MarkerInterceptor.InstanceCount = 0;
            MarkerInterceptor.SaveCount = 0;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MarkerInterceptor>();
            services.ConfigureDbContext<PrototypeDbContext>((sp, opt) =>
            {
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString());
                opt.AddInterceptors(sp.GetRequiredService<MarkerInterceptor>());
            });
            services.AddDbContext<PrototypeDbContext>((sp, opt) => opt.UseInMemoryDatabase("g1"));

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<PrototypeDbContext>();

            // The interceptor must be attached even though the user's AddDbContext delegate
            // never mentioned it, and its save hook must actually run.
            context.Add(new PrototypeEntity { Name = "x" });
            await context.SaveChangesAsync();
            Assert.Equal(1, MarkerInterceptor.InstanceCount);
            Assert.Equal(1, MarkerInterceptor.SaveCount);
        }

        [Fact]
        public async Task Gate2_ConfigureDbContext_WithPool_AttachesInterceptor()
        {
            MarkerInterceptor.InstanceCount = 0;
            MarkerInterceptor.SaveCount = 0;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MarkerInterceptor>();
            services.ConfigureDbContext<PrototypeDbContext>((sp, opt) =>
            {
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString());
                opt.AddInterceptors(sp.GetRequiredService<MarkerInterceptor>());
            });
            services.AddDbContextPool<PrototypeDbContext>((sp, opt) => opt.UseInMemoryDatabase("g2"));

            await using var provider = services.BuildServiceProvider();
            for (var i = 0; i < 3; i++)
            {
                await using var scope = provider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<PrototypeDbContext>();
                context.Add(new PrototypeEntity { Name = "p" + i });
                await context.SaveChangesAsync();
            }

            // Exactly one interceptor instance exists: the singleton was reused across the
            // pooled context leases (and no per-lease instance was created). Its save hook
            // ran for every lease.
            Assert.Equal(1, MarkerInterceptor.InstanceCount);
            Assert.Equal(3, MarkerInterceptor.SaveCount);
        }

        [Fact]
        public async Task Gate3_SameInterceptorInstance_ReusedAcrossScopes()
        {
            MarkerInterceptor.InstanceCount = 0;
            MarkerInterceptor.SaveCount = 0;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MarkerInterceptor>();
            services.ConfigureDbContext<PrototypeDbContext>((sp, opt) =>
            {
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString());
                opt.AddInterceptors(sp.GetRequiredService<MarkerInterceptor>());
            });
            services.AddDbContext<PrototypeDbContext>((sp, opt) => opt.UseInMemoryDatabase("g3"));

            await using var provider = services.BuildServiceProvider();

            // A singleton marker is resolved exactly once by the container; the configurator
            // attaches that same instance to every context configuration. InstanceCount == 1
            // proves no per-scope / per-lease instance was ever created (which would also
            // trigger EF's ManyServiceProvidersCreatedWarning).
            for (var i = 0; i < 3; i++)
            {
                await using var scope = provider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<PrototypeDbContext>();
                context.Add(new PrototypeEntity { Name = "s" + i });
                await context.SaveChangesAsync();
            }

            Assert.Equal(1, MarkerInterceptor.InstanceCount);
            Assert.Equal(3, MarkerInterceptor.SaveCount);
        }

        [Fact]
        public async Task Gate4_ConfigureDbContext_ComposesWithUserDelegate()
        {
            MarkerInterceptor.InstanceCount = 0;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MarkerInterceptor>();
            services.ConfigureDbContext<PrototypeDbContext>((sp, opt) =>
                opt.AddInterceptors(sp.GetRequiredService<MarkerInterceptor>()));
            services.AddDbContext<PrototypeDbContext>((sp, opt) =>
                opt.UseInMemoryDatabase("g4"));

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<PrototypeDbContext>();

            // The user delegate supplied the provider; the configurator supplied the
            // interceptor — both compose (non-conflicting options merged).
            context.Add(new PrototypeEntity { Name = "y" });
            await context.SaveChangesAsync();
            Assert.Equal(1, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task Gate5_ConfigureDbContext_ComposesWithUserInstalledInterceptors()
        {
            MarkerInterceptor.InstanceCount = 0;
            UserInterceptor.InstanceCount = 0;
            UserInterceptor.SaveCount = 0;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MarkerInterceptor>();
            services.AddSingleton<UserInterceptor>();
            services.ConfigureDbContext<PrototypeDbContext>((sp, opt) =>
            {
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString());
                opt.AddInterceptors(sp.GetRequiredService<MarkerInterceptor>());
            });
            services.AddDbContext<PrototypeDbContext>((sp, opt) =>
            {
                opt.UseInMemoryDatabase("g5");
                // The user installs their own interceptor inside AddDbContext; the
                // configurator's interceptor must compose (AddInterceptors is additive).
                opt.AddInterceptors(sp.GetRequiredService<UserInterceptor>());
            });

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<PrototypeDbContext>();

            context.Add(new PrototypeEntity { Name = "both" });
            await context.SaveChangesAsync();

            // Both the configurator-installed and the user-installed interceptor ran.
            Assert.Equal(1, MarkerInterceptor.InstanceCount);
            Assert.Equal(1, MarkerInterceptor.SaveCount);
            Assert.Equal(1, UserInterceptor.InstanceCount);
            Assert.Equal(1, UserInterceptor.SaveCount);
        }
    }
}
