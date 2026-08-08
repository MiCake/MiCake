using MiCake.EntityFrameworkCore;
using MiCake.Audit.Core;
// using MiCake.Audit.SoftDeletion; // already imported below
using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MiCake.Core;
using MiCake.Core.Modularity;
using System;
using MiCake.IntegrationTests.Fixtures;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Uow
{
    [Collection("MiCakeIntegrationTests")]
    public class AuditIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IMiCakeApplication _miCakeApp;
        private readonly MiCakeAppFixture _fixture;

        public AuditIntegrationTests(MiCakeAppFixture fixture)
        {
            _fixture = fixture;
            _serviceProvider = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var dbName = Guid.NewGuid().ToString();
                // Register EF Core DbContext and MiCake services
                services.AddDbContext<TestDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });

                // Use MiCake builder to register EFCore and Audit modules (real implementations)
                var builder = services.AddMiCake<TestMiCakeModule>();
                builder.UseEFCore<TestDbContext>();
                builder.UseAudit(opts => opts.UseSoftDeletion = true);
                builder.Build();
            });

            // Initialize MiCake application so modules register internal lifetimes
            // App will be started by fixture when creating provider
            _miCakeApp = _serviceProvider.GetRequiredService<IMiCakeApplication>();
        }

        /// <summary>
        /// Runs the test body inside a scope with an active writable unit of work.
        /// The MiCake write guard rejects every framework-mediated write without an ambient
        /// writable UoW, so audit behavior is asserted within the contracted boundary.
        /// </summary>
        private Task RunInUowAsync(Func<TestDbContext, Task> action)
            => RunInUowAsync(_serviceProvider, action);

        private static async Task RunInUowAsync(IServiceProvider provider, Func<TestDbContext, Task> action)
        {
            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await manager.BeginAsync();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await action(dbContext);
            await uow.CommitAsync();
        }

        [Fact]
        public async Task SaveChanges_ShouldSetCreationTime_ForEntitiesWithIHasCreatedAt()
        {
            await RunInUowAsync(async dbContext =>
            {
                // Arrange
                var entity = new AuditEntityWithCreationTime { Name = "Create" };
                dbContext.AuditEntities.Add(entity);

                // Act
                await dbContext.SaveChangesAsync();

                // Assert
                Assert.NotEqual(default, entity.CreatedAt);
            });
        }

        [Fact]
        public async Task SaveChanges_ShouldSetModificationTime_ForEntitiesWithIHasUpdatedAt()
        {
            await RunInUowAsync(async dbContext =>
            {
                // Arrange
                var entity = new AuditEntityWithCreateAndModify { Name = "Initial" };
                dbContext.AuditEntities2.Add(entity);
                await dbContext.SaveChangesAsync();
                dbContext.ChangeTracker.Clear();

                // Act - update then save
                var existing = await dbContext.AuditEntities2.FindAsync(entity.Id);
                existing.Name = "Updated";
                await dbContext.SaveChangesAsync();

                // Assert
                Assert.NotNull(existing.UpdatedAt);
            });
        }

        [Fact]
        public async Task SaveChanges_DeleteEntityWithSoftDeletion_ShouldSetIsDeletedAndRemainInStore()
        {
            // Ensure audit providers and pre-save lifetimes are registered
            var auditProviders = _serviceProvider.GetServices<IAuditProvider>().ToList();
            Assert.NotEmpty(auditProviders);
            Assert.Contains(auditProviders, p => p.GetType().FullName == "MiCake.Audit.SoftDeletion.SoftDeletionAuditProvider");

            var auditExecutor = _serviceProvider.GetService<IAuditExecutor>();
            Assert.NotNull(auditExecutor);

            var saveChangeLifetime = _serviceProvider.GetService<IEFSaveChangesLifetime>();
            Assert.NotNull(saveChangeLifetime);

            // Ensure soft deletion lifetime is registered
            var preHandlers = _serviceProvider.GetServices<DDD.Infrastructure.Lifetime.IRepositoryPreSaveChanges>().ToList();
            var moduleContext = _serviceProvider.GetService<IMiCakeModuleContext>();
            Assert.NotNull(moduleContext);
            Assert.NotEmpty(moduleContext.MiCakeModules);
            Assert.True(preHandlers.Count > 0, $"preHandlers count = {preHandlers.Count}");
            Assert.Contains(preHandlers, h => h.GetType().FullName == "MiCake.Audit.Lifetime.SoftDeletionRepositoryLifetime");

            // Arrange
            var entity = new SoftDeletableEntity { Name = "ToDelete" };

            await RunInUowAsync(async dbContext =>
            {
                dbContext.SoftEntities.Add(entity);
                await dbContext.SaveChangesAsync();

                // Act - remove and save
                dbContext.SoftEntities.Remove(entity);
                await dbContext.SaveChangesAsync();

                // Assert - entity still exists in store but marked as deleted
                var fetched = await dbContext.SoftEntities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entity.Id);
                Assert.NotNull(fetched);
                Assert.True(fetched.IsDeleted);
            });
        }

        [Fact]
        public async Task DefaultTimeAuditProvider_ShouldUseCustomTimeProvider()
        {
            // Arrange - Create a new scope with custom TimeProvider
            var fixedTime = new DateTime(2025, 1, 1, 12, 0, 0);
            var customScope = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<TestDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });

                // Register custom TimeProvider
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(fixedTime)));

                var builder = services.AddMiCake<TestMiCakeModule>();
                builder.UseEFCore<TestDbContext>();
                builder.UseAudit(opts => opts.UseSoftDeletion = true);
                builder.Build();
            });

            try
            {
                var scopedApp = customScope.GetRequiredService<IMiCakeApplication>();

                var entity = new AuditEntityWithCreationTime { Name = "CustomTime" };
                await RunInUowAsync(customScope, async scopedDb =>
                {
                    scopedDb.AuditEntities.Add(entity);
                    await scopedDb.SaveChangesAsync();
                });

                Assert.Equal(new DateTimeOffset(fixedTime).UtcDateTime, entity.CreatedAt);
            }
            finally
            {
                _fixture?.ReleaseServiceProvider(customScope);
            }
        }

        public void Dispose()
        {
            // Release provider and ensure MiCake app is shutdown via fixture
            _fixture?.ReleaseServiceProvider(_serviceProvider);
        }

        #region Test Infrastructure

        [RelyOn(typeof(EntityFrameworkCore.Modules.MiCakeEFCoreModule), typeof(Modules.MiCakeEssentialModule))]
        private class TestMiCakeModule : MiCakeModule
        {
            // empty on purpose - we just need an entry module to trigger the MiCake module discovery
        }

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<AuditEntityWithCreationTime> AuditEntities { get; set; }
            public DbSet<AuditEntityWithCreateAndModify> AuditEntities2 { get; set; }
            public DbSet<SoftDeletableEntity> SoftEntities { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.UseMiCakeConventions();
            }
        }

        private class AuditEntityWithCreationTime : DDD.Domain.AggregateRoot<Guid>, IHasCreatedAt
        {
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private class AuditEntityWithCreateAndModify : DDD.Domain.AggregateRoot<Guid>, IHasCreatedAt, IHasUpdatedAt
        {
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        private class SoftDeletableEntity : DDD.Domain.AggregateRoot<Guid>, ISoftDeletable, IAuditableWithSoftDeletion
        {
            public string Name { get; set; }
            public bool IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        // Fake TimeProvider for testing
        private class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _fixedTime;

            public FakeTimeProvider(DateTimeOffset fixedTime)
            {
                _fixedTime = fixedTime;
            }

            public override DateTimeOffset GetUtcNow() => _fixedTime;
        }

        #endregion
    }
}
