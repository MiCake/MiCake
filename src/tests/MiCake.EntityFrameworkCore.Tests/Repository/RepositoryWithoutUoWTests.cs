using MiCake.DDD.Domain;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Repository
{
    /// <summary>
    /// Repository DbContext access without an explicit unit of work.
    /// Context identity is owned by the frame-stable factory: without an active UoW the
    /// factory rejects access by default, and explicit AllowDbContextAccessWithoutUoW permits
    /// read-only fallback access. Writes remain guarded regardless of the option. Repositories
    /// hold no per-UoW context cache of their own.
    /// </summary>
    public class RepositoryWithoutUoWTests : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly TestDbContext _dbContext;
        private readonly Mock<IUnitOfWorkManager> _mockUnitOfWorkManager;
        private readonly Mock<ILogger<EFRepositoryDependencies<TestDbContext>>> _mockDependenciesLogger;

        public RepositoryWithoutUoWTests()
        {
            _mockUnitOfWorkManager = new Mock<IUnitOfWorkManager>();
            _mockUnitOfWorkManager.Setup(um => um.Current).Returns((IUnitOfWork)null);
            _mockDependenciesLogger = new Mock<ILogger<EFRepositoryDependencies<TestDbContext>>>();

            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddLogging();
            _provider = services.BuildServiceProvider();
            _dbContext = _provider.GetRequiredService<TestDbContext>();
        }

        #region Real Factory Contract Tests (No UoW)

        [Fact]
        public void DbContext_WithoutActiveUoW_Default_ThrowsWithGuidance()
        {
            var repository = CreateRepository(bypass: false);

            var exception = Assert.Throws<InvalidOperationException>(() => repository.DbContext);

            Assert.Contains("No active Unit of Work", exception.Message);
        }

        [Fact]
        public async Task GetDbContextAsync_WithoutActiveUoW_Default_Throws()
        {
            var repository = CreateRepository(bypass: false);

            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetDbContextAsync());
        }

        [Fact]
        public void DbContext_WithoutActiveUoW_WhenAccessAllowed_ResolvesScopedContext()
        {
            var repository = CreateRepository(bypass: true);

            var context = repository.DbContext;

            Assert.Same(_dbContext, context);
        }

        [Fact]
        public void DbContext_AccessedMultipleTimes_WhenAccessAllowed_ResolvesSameScopedContext()
        {
            var repository = CreateRepository(bypass: true);

            var context1 = repository.DbContext;
            var context2 = repository.DbContext;

            Assert.Same(_dbContext, context1);
            Assert.Same(context1, context2);
        }

        [Fact]
        public void DbSet_WithoutActiveUoW_WhenAccessAllowed_ReturnsDbSet()
        {
            var repository = CreateRepository(bypass: true);

            Assert.NotNull(repository.DbSet);
        }

        [Fact]
        public void Entities_WithoutActiveUoW_WhenAccessAllowed_ReturnsQueryable()
        {
            var repository = CreateRepository(bypass: true);

            Assert.NotNull(repository.Entities);
            Assert.NotNull(repository.EntitiesNoTracking);
        }

        private TestRepository CreateRepository(bool bypass)
        {
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext)) { AllowDbContextAccessWithoutUoW = bypass };
            var factory = new EFCoreContextFactory<TestDbContext>(
                _provider,
                _mockUnitOfWorkManager.Object,
                _provider.GetRequiredService<ILogger<EFCoreContextFactory<TestDbContext>>>(),
                options);
            var dependencies = new EFRepositoryDependencies<TestDbContext>(
                factory, _mockUnitOfWorkManager.Object, _mockDependenciesLogger.Object, options);
            return new TestRepository(dependencies);
        }

        #endregion

        #region Frame-Stable Delegation Tests

        [Fact]
        public void DbContext_AccessedMultipleTimes_ResolvesFromFactoryPerAccess()
        {
            var mockContextFactory = new Mock<IEFCoreContextFactory<TestDbContext>>();
            mockContextFactory.Setup(f => f.GetDbContext()).Returns(_dbContext);
            var options = new MiCakeEFCoreOptions(typeof(TestDbContext));
            var dependencies = new EFRepositoryDependencies<TestDbContext>(
                mockContextFactory.Object, _mockUnitOfWorkManager.Object, _mockDependenciesLogger.Object, options);
            var repository = new TestRepository(dependencies);

            var result1 = repository.DbContext;
            var result2 = repository.DbContext;
            var result3 = repository.DbContext;

            Assert.Same(result1, result2);
            Assert.Same(result2, result3);
            mockContextFactory.Verify(f => f.GetDbContext(), Times.Exactly(3));
        }

        #endregion

        #region Helper Classes

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<TestEntity> TestEntities { get; set; }
        }

        public class TestEntity : Entity<int>
        {
            public string Name { get; set; }
        }

        public class TestRepository : EFRepositoryBase<TestDbContext, TestEntity, int>
        {
            public TestRepository(EFRepositoryDependencies<TestDbContext> dependencies)
                : base(dependencies)
            {
            }

            public new TestDbContext DbContext => base.DbContext;
            public new DbSet<TestEntity> DbSet => base.DbSet;
            public new IQueryable<TestEntity> Entities => base.Entities;
            public new IQueryable<TestEntity> EntitiesNoTracking => base.EntitiesNoTracking;

            public new Task<TestDbContext> GetDbContextAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return base.GetDbContextAsync(cancellationToken);
            }
        }

        #endregion

        public void Dispose()
        {
            _provider?.Dispose();
        }
    }
}
