using MiCake.Audit.SoftDeletion;
using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore;
using MiCake.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Uow
{
    /// <summary>
    /// Verifies the soft-delete query filter in a started MiCake host, where module initialization
    /// resolves the registered DbContext before the application uses it.
    /// </summary>
    [Collection("MiCakeIntegrationTests")]
    public class SoftDeleteQueryFilterIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly MiCakeAppFixture _fixture;

        public SoftDeleteQueryFilterIntegrationTests(MiCakeAppFixture fixture)
        {
            _fixture = fixture;
            _serviceProvider = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<SoftDeleteHostDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });

                var builder = services.AddMiCake<SoftDeleteHostModule>();
                builder.UseEFCore<SoftDeleteHostDbContext>();
                builder.UseAudit(opts => opts.UseSoftDeletion = true);
                builder.Build();
            });
        }

        public void Dispose()
        {
            _fixture?.ReleaseServiceProvider(_serviceProvider);
        }

        [Fact]
        public void StartedHost_ModelHasTheNamedSoftDeleteFilter()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SoftDeleteHostDbContext>();

            var filter = Assert.Single(dbContext.Model.FindEntityType(typeof(SoftDeleteHostEntity)).GetDeclaredQueryFilters());
            Assert.Equal(MiCakeQueryFilterNames.SoftDelete, filter.Key);
        }

        [Fact]
        public async Task StartedHost_SoftDeletedEntityIsHiddenFromNormalQueries()
        {
            Guid deletedId;
            await using (var scope = _serviceProvider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var dbContext = scope.ServiceProvider.GetRequiredService<SoftDeleteHostDbContext>();

                var kept = new SoftDeleteHostEntity { Name = "kept" };
                var deleted = new SoftDeleteHostEntity { Name = "deleted" };
                dbContext.Entities.AddRange(kept, deleted);
                await dbContext.SaveChangesAsync();

                dbContext.Entities.Remove(deleted);
                await dbContext.SaveChangesAsync();
                await uow.CommitAsync();
                deletedId = deleted.Id;
            }

            await using (var scope = _serviceProvider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var dbContext = scope.ServiceProvider.GetRequiredService<SoftDeleteHostDbContext>();

                var visible = await dbContext.Entities.Select(e => e.Name).ToListAsync();
                Assert.Equal(new[] { "kept" }, visible);

                var stored = await dbContext.Entities
                    .IgnoreQueryFilters(new[] { MiCakeQueryFilterNames.SoftDelete })
                    .SingleAsync(e => e.Id == deletedId);
                Assert.True(stored.IsDeleted);
                await uow.CommitAsync();
            }
        }

        [RelyOn(typeof(EntityFrameworkCore.Modules.MiCakeEFCoreModule), typeof(Modules.MiCakeEssentialModule))]
        private class SoftDeleteHostModule : MiCakeModule
        {
        }

        private class SoftDeleteHostDbContext : MiCakeDbContext
        {
            public SoftDeleteHostDbContext(DbContextOptions<SoftDeleteHostDbContext> options) : base(options)
            {
            }

            public DbSet<SoftDeleteHostEntity> Entities { get; set; }
        }

        private class SoftDeleteHostEntity : DDD.Domain.AggregateRoot<Guid>, ISoftDeletable
        {
            public string Name { get; set; }
            public bool IsDeleted { get; set; }
        }
    }
}
