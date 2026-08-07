using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Internal
{
    /// <summary>
    /// End-to-end write-guard tests through the installed interceptors:
    /// SaveChanges, ExecuteSqlRaw, and ExecuteDelete are rejected without a writable
    /// unit of work and participate in the active UoW transaction when present.
    /// </summary>
    public class MiCakeWriteGuardInterceptorsTests : IDisposable
    {
        private readonly string _dbPath;

        public MiCakeWriteGuardInterceptorsTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"micake-writeguard-{Guid.NewGuid():N}.db");
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        private ServiceProvider BuildProvider(bool allowDbContextAccessWithoutUoW = false)
        {
            var services = new ServiceCollection();
            services.AddDbContext<WriteGuardTestDbContext>((sp, opt) =>
                opt.UseSqlite($"Data Source={_dbPath};Pooling=False")
                   .UseMiCakeInterceptors(sp));
            services.AddLogging();
            services.AddUowCoreServices(typeof(WriteGuardTestDbContext));

            var ambientAccessorType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.AmbientUnitOfWorkAccessor");
            services.AddSingleton(ambientAccessorType!);
            services.AddSingleton<IUnitOfWorkAmbientAccessor>(sp => (IUnitOfWorkAmbientAccessor)sp.GetRequiredService(ambientAccessorType!));
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType!);
            services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(
                new MiCakeEFCoreOptions(typeof(WriteGuardTestDbContext))
                {
                    AllowDbContextAccessWithoutUoW = allowDbContextAccessWithoutUoW
                });

            // Save lifecycle is a t4 concern; the write guard only needs a non-null lifetime.
            services.AddSingleton<IEFSaveChangesLifetime>(Mock.Of<IEFSaveChangesLifetime>());
            services.AddSingleton<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IDbContextTypeRegistry>().RegisterDbContextType(typeof(WriteGuardTestDbContext));
            return provider;
        }

        [Fact]
        public async Task SaveChangesAsync_WithoutUoW_WhenAccessAllowed_StillThrows()
        {
            // The access option only relaxes context resolution; the write guard is orthogonal
            // and must still reject a write without an active writable unit of work.
            using var provider = BuildProvider(allowDbContextAccessWithoutUoW: true);
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            await context.Database.EnsureCreatedAsync();
            context.Add(new WriteGuardEntity { Name = "no-uow" });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("active writable unit of work", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SaveChangesAsync_WithoutUoW_ShouldThrow()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            await context.Database.EnsureCreatedAsync();
            context.Add(new WriteGuardEntity { Name = "no-uow" });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("active writable unit of work", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SaveChangesAsync_WithinUoW_RollbackDiscardsWrites()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new WriteGuardEntity { Name = "rolled back" });
            await context.SaveChangesAsync();
            await uow.RollbackAsync();

            // Assert
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task SaveChangesAsync_WithinUoW_CommitPersistsWrites()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            context.Add(new WriteGuardEntity { Name = "committed" });
            await context.SaveChangesAsync();
            await uow.CommitAsync();

            // Assert
            Assert.Equal(1, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task ExecuteSqlRawAsync_WithoutUoW_ShouldThrow()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            await context.Database.EnsureCreatedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                context.Database.ExecuteSqlRawAsync("INSERT INTO Entities (Name) VALUES ('raw')"));

            Assert.Contains("active writable unit of work", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExecuteSqlRawAsync_WithinUoW_RollbackDiscardsWrite()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            await using var uow = await uowManager.BeginAsync();
            await context.Database.ExecuteSqlRawAsync("INSERT INTO Entities (Name) VALUES ('raw')");
            await uow.RollbackAsync();

            // Assert
            Assert.Equal(0, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task ExecuteDeleteAsync_WithoutUoW_ShouldThrow()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            await context.Database.EnsureCreatedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                context.Entities.ExecuteDeleteAsync());

            Assert.Contains("active writable unit of work", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SaveChanges_WithoutProvider_ThrowsWithGuidance()
        {
            var options = new DbContextOptionsBuilder<NoProviderWriteGuardDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new NoProviderWriteGuardDbContext(options);
            context.Add(new WriteGuardEntity { Name = "no provider" });

            // The parameterless interceptor path cannot resolve the write pipeline; the
            // write is rejected with guidance instead of silently bypassing the UoW.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("UseMiCakeInterceptors(IServiceProvider)", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExecuteDeleteAsync_WithinUoW_IsVisibleInTransactionAndRollsBackWithIt()
        {
            using var provider = BuildProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<WriteGuardTestDbContext>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await context.Database.EnsureCreatedAsync();

            // Persist two rows in a first, committed unit of work.
            await using (var firstUow = await uowManager.BeginAsync())
            {
                context.AddRange(
                    new WriteGuardEntity { Name = "a" },
                    new WriteGuardEntity { Name = "b" });
                await context.SaveChangesAsync();
                await firstUow.CommitAsync();
            }

            Assert.Equal(2, await context.Entities.CountAsync());

            // Delete inside a second unit of work and roll it back.
            await using var secondUow = await uowManager.BeginAsync();
            await context.Entities.ExecuteDeleteAsync();

            // The bulk delete is visible inside the transaction but not yet durable.
            Assert.Equal(0, await context.Entities.CountAsync());

            await secondUow.RollbackAsync();

            // Rolling back restores the previously committed rows: the delete was transactional.
            Assert.Equal(2, await context.Entities.CountAsync());
        }
    }

    /// <summary>
    /// DbContext configured through the parameterless interceptor overload, which cannot
    /// resolve the MiCake write pipeline; writes must fail fast with guidance.
    /// </summary>
    public class NoProviderWriteGuardDbContext : DbContext
    {
        public NoProviderWriteGuardDbContext(DbContextOptions<NoProviderWriteGuardDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseMiCakeInterceptors();
        }

        public DbSet<WriteGuardEntity> Entities => Set<WriteGuardEntity>();
    }
}
