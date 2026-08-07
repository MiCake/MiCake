using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using MiCake.Util.Query.Dynamic;
using MiCake.Util.Query.Paging;

namespace MiCake.IntegrationTests.Repository
{
    public class CommonFilterPagingQueryIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _provider;

        public CommonFilterPagingQueryIntegrationTests()
        {
            var services = new ServiceCollection();

            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>((sp, opt) =>
            {
                opt.UseInMemoryDatabase(dbName);
                opt.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                opt.UseMiCakeInterceptors(sp);
            });

            services.AddLogging();

            // UoW runtime internals registered directly (the IntegrationTests assembly has
            // InternalsVisibleTo access to the framework packages).
            services.AddSingleton<AmbientUnitOfWorkAccessor>();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();

            // Register EF Core factory and repository dependency wrapper so repository can be constructed from DI
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));
            services.AddScoped(typeof(MiCake.EntityFrameworkCore.Repository.EFRepositoryDependencies<TestDbContext>));
            services.AddSingleton<Core.DependencyInjection.IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(TestDbContext)));

            // Register repo
            services.AddScoped<TestPagingRepository>();

            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task CommonFilterPagingQueryAsync_EndToEnd_ReturnsFilteredCountAndPage()
        {
            // Arrange: seed data inside a writable unit of work (the write guard rejects
            // framework-mediated writes without an ambient UoW).
            await using (var seedScope = _provider.CreateAsyncScope())
            {
                var seedManager = seedScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var seedUow = await seedManager.BeginAsync();
                var seedContext = seedScope.ServiceProvider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>().GetDbContext();
                for (int i = 0; i < 30; i++)
                {
                    seedContext.TestEntities.Add(new TestEntity { Name = i % 3 == 0 ? $"match-{i}" : $"other-{i}" });
                }
                await seedUow.CommitAsync();
            }

            await using var scope = _provider.CreateAsyncScope();

            // Resolve UoW manager and begin UoW
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            await using var uow = await uowManager.BeginAsync(new UnitOfWorkOptions { InitializationMode = TransactionInitializationMode.Immediate });

            // Ensure EF factory registers wrapper with UoW
            var factory = scope.ServiceProvider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>();
            factory.GetOrCreateWrapperFor(factory.GetDbContext());

            // Act: use repository from DI
            var repo = scope.ServiceProvider.GetRequiredService<TestPagingRepository>();
            var filter = Filter.Create(nameof(TestEntity.Name), new System.Collections.Generic.List<FilterValue> { FilterValue.Create("match", ValueOperatorType.Contains) });
            var response = await repo.FilterPagingQueryAsync(new PagingRequest(1, 20), FilterGroup.Create(new System.Collections.Generic.List<Filter> { filter }));

            // Assert
            Assert.Equal(1, response.CurrentIndex);
            // There should be all items where Name contains 'match'
            var dbContext = factory.GetDbContext();
            Assert.Equal(dbContext.TestEntities.Count(e => e.Name.Contains("match")), response.TotalCount);
            Assert.True(response.Data.Count() <= 20);
        }

        public void Dispose()
        {
            _provider?.Dispose();
        }

        #region Test types

        public class TestDbContext : MiCakeDbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> opts) : base(opts) { }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                // WORKAROUND for defect found during t7 acceptance testing:
                // MiCakeDbContext.OnConfiguring installs a provider-less fallback write-guard
                // interceptor that conflicts with UseMiCakeInterceptors(sp); skip the base call
                // until the defect is fixed (see test-design.md in the t7 artifacts).
            }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        public class TestEntity : MiCake.DDD.Domain.AggregateRoot<int>
        {
            public string Name { get; set; }
        }

        public class TestPagingRepository : EFRepositoryHasPaging<TestDbContext, TestEntity, int>
        {
            public TestPagingRepository(MiCake.EntityFrameworkCore.Repository.EFRepositoryDependencies<TestDbContext> deps) : base(deps) { }
        }

        #endregion
    }
}
