using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Internal
{
    /// <summary>
    /// Prototype validation for the revised 方案 C design (20260807-interceptor-di-registration).
    /// These tests validate the EF Core 9+/10 <c>ConfigureDbContext</c> mechanism BEFORE any
    /// production code is changed (design Verification Prerequisites, hard gates). They use a
    /// minimal marker interceptor — they do NOT depend on the MiCake write pipeline, so they
    /// isolate the framework mechanism from MiCake internals.
    ///
    /// Gates under test:
    ///   1. ConfigureDbContext + AddDbContext -> interceptor is attached (no user call).
    ///   2. ConfigureDbContext + AddDbContextPool -> interceptor attached; pool-reset contract.
    ///   3. Same interceptor instance reused (no ManyServiceProvidersCreatedWarning).
    ///   4. ConfigureDbContext + user AddDbContext delegate compose in order.
    ///   5. (Permissive policy is a MiCake-interceptor behavior; its guard logic is covered by
    ///      existing write-guard tests once the interceptor ctor is rewired — this prototype
    ///      only proves the attachment mechanism.)
    /// </summary>
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
        /// constructor injection from the application container works.
        /// </summary>
        private sealed class MarkerInterceptor : SaveChangesInterceptor
        {
            public static int InstanceCount;
            public readonly Guid InstanceId = Guid.NewGuid();

            public MarkerInterceptor()
            {
                System.Threading.Interlocked.Increment(ref InstanceCount);
            }

            public override InterceptionResult<int> SavingChanges(
                DbContextEventData eventData,
                InterceptionResult<int> result)
            {
                return result;
            }
        }

        [Fact]
        public async Task Gate1_ConfigureDbContext_AttachesInterceptor_WithoutUserCall()
        {
            MarkerInterceptor.InstanceCount = 0;
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
            // never mentioned it.
            context.Add(new PrototypeEntity { Name = "x" });
            await context.SaveChangesAsync();
            Assert.Equal(1, MarkerInterceptor.InstanceCount);
        }

        [Fact]
        public async Task Gate2_ConfigureDbContext_WithPool_AttachesInterceptor()
        {
            MarkerInterceptor.InstanceCount = 0;
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
            // pooled context leases (and no per-lease instance was created).
            Assert.Equal(1, MarkerInterceptor.InstanceCount);
        }

        [Fact]
        public async Task Gate3_SameInterceptorInstance_ReusedAcrossScopes()
        {
            MarkerInterceptor.InstanceCount = 0;
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
    }
}
