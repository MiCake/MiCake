using MiCake.EntityFrameworkCore;
using MiCake.Audit.Core;
// using MiCake.Audit.SoftDeletion; // already imported below
using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
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
        private readonly TestDbContext _dbContext;
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

                    // Prefer DI-resolved factory to create interceptors; fall back to old static helper
                    options.UseMiCakeInterceptors(sp);
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

            _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        }

        [Fact]
        public async Task SaveChanges_ShouldSetCreationTime_ForEntitiesWithIHasCreationTime()
        {
            // Arrange
            var entity = new AuditEntityWithCreationTime { Name = "Create" };
            _dbContext.AuditEntities.Add(entity);

            // Act
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotEqual(default, entity.CreationTime);
        }

        [Fact]
        public async Task SaveChanges_ShouldSetModificationTime_ForEntitiesWithIHasModificationTime()
        {
            // Arrange
            var entity = new AuditEntityWithCreateAndModify { Name = "Initial" };
            _dbContext.AuditEntities2.Add(entity);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act - update then save
            var existing = await _dbContext.AuditEntities2.FindAsync(entity.Id);
            existing.Name = "Updated";
            await _dbContext.SaveChangesAsync();

            // Assert
            Assert.NotNull(existing.ModificationTime);
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
            _dbContext.SoftEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Act - remove and save
            _dbContext.SoftEntities.Remove(entity);
            await _dbContext.SaveChangesAsync();

            // Assert - entity still exists in store but marked as deleted
            var fetched = await _dbContext.SoftEntities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entity.Id);
            Assert.NotNull(fetched);
            Assert.True(fetched.IsDeleted);
        }

        [Fact]
        public async Task DefaultTimeAuditProvider_ShouldUseCustomTimeProvider()
        {
            var fixedTime = new DateTime(2025, 1, 1, 12, 0, 0);
            var originalProvider = DefaultTimeAuditProvider.CurrentTimeProvider;
            try
            {
                DefaultTimeAuditProvider.CurrentTimeProvider = () => fixedTime;

                var entity = new AuditEntityWithCreationTime { Name = "CustomTime" };
                _dbContext.AuditEntities.Add(entity);
                await _dbContext.SaveChangesAsync();

                Assert.Equal(fixedTime, entity.CreationTime);
            }
            finally
            {
                DefaultTimeAuditProvider.CurrentTimeProvider = originalProvider;
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

        private class AuditEntityWithCreationTime : DDD.Domain.AggregateRoot<Guid>, IHasCreationTime
        {
            public string Name { get; set; }
            public DateTime CreationTime { get; set; }
        }

        private class AuditEntityWithCreateAndModify : DDD.Domain.AggregateRoot<Guid>, IHasCreationTime, IHasModificationTime
        {
            public string Name { get; set; }
            public DateTime CreationTime { get; set; }
            public DateTime? ModificationTime { get; set; }
        }

        private class SoftDeletableEntity : DDD.Domain.AggregateRoot<Guid>, ISoftDeletion, IHasAuditWithSoftDeletion
        {
            public string Name { get; set; }
            public bool IsDeleted { get; set; }
            public DateTime? DeletionTime { get; set; }
            public DateTime CreationTime { get; set; }
            public DateTime? ModificationTime { get; set; }
        }


        #endregion
    }
}
