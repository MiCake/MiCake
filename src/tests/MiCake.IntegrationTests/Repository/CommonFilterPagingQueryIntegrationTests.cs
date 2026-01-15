using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.DDD.Uow;
using MiCake.Util.Query.Dynamic;
using MiCake.Util.Query.Paging;

namespace MiCake.IntegrationTests.Repository
{
    public class CommonFilterPagingQueryIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly TestDbContext _dbContext;

        public CommonFilterPagingQueryIntegrationTests()
        {
            var services = new ServiceCollection();

            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<TestDbContext>(opt =>
            {
                opt.UseInMemoryDatabase(dbName);
                opt.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddLogging();

            // Register UnitOfWork manager internal implementation via reflection to match other integration tests
            var uowManagerType = typeof(IUnitOfWorkManager).Assembly.GetType("MiCake.DDD.Uow.Internal.UnitOfWorkManager");
            services.AddScoped(typeof(IUnitOfWorkManager), uowManagerType);

            // Register EF Core factory and repository dependency wrapper so repository can be constructed from DI
            services.AddScoped(typeof(IEFCoreContextFactory<TestDbContext>), typeof(EFCoreContextFactory<TestDbContext>));
            services.AddScoped(typeof(MiCake.EntityFrameworkCore.Repository.EFRepositoryDependencies<TestDbContext>));
            services.AddSingleton<Core.DependencyInjection.IObjectAccessor<MiCakeEFCoreOptions>>(new MiCakeEFCoreOptions(typeof(TestDbContext)));

            // Register repo
            services.AddScoped<TestPagingRepository>();

            _provider = services.BuildServiceProvider();
            _dbContext = _provider.GetRequiredService<TestDbContext>();
        }

        [Fact]
        public async Task CommonFilterPagingQueryAsync_EndToEnd_ReturnsFilteredCountAndPage()
        {
            // Arrange: Seed data
            for (int i = 0; i < 30; i++)
            {
                _dbContext.TestEntities.Add(new TestEntity { Name = i % 3 == 0 ? $"match-{i}" : $"other-{i}" });
            }
            await _dbContext.SaveChangesAsync();

            // Resolve UoW manager and begin UoW
            var uowManager = _provider.GetRequiredService<IUnitOfWorkManager>();
            using var uow = await uowManager.BeginAsync(new UnitOfWorkOptions { Strategy = PersistenceStrategy.TransactionManaged, InitializationMode = TransactionInitializationMode.Immediate });

            // Ensure EF factory registers wrapper with UoW
            var factory = _provider.GetRequiredService<IEFCoreContextFactory<TestDbContext>>();
            var wrapper = factory.GetDbContextWrapper();

            // Act: use repository from DI
            var repo = _provider.GetRequiredService<TestPagingRepository>();
            var filter = Filter.Create(nameof(TestEntity.Name), new System.Collections.Generic.List<FilterValue> { FilterValue.Create("match", ValueOperatorType.Contains) });
            var response = await repo.FilterPagingQueryAsync(new PagingRequest(1, 20), FilterGroup.Create(new System.Collections.Generic.List<Filter> { filter }));

            // Assert
            Assert.Equal(1, response.CurrentIndex);
            // There should be all items where Name contains 'match'
            Assert.Equal(_dbContext.TestEntities.Count(e => e.Name.Contains("match")), response.TotalCount);
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
