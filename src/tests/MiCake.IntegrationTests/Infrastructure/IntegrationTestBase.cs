using MiCake.Audit;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using MiCake.Core;
using MiCake.Core.Data;

namespace MiCake.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Base class for integration tests with full MiCake framework setup
    /// </summary>
    public abstract class IntegrationTestBase : IDisposable
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        protected IUnitOfWorkManager UowManager { get; private set; }

        private readonly SqliteConnection _connection;
        private readonly IMiCakeApplication _micakeApp;

        protected IntegrationTestBase()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            // Build MiCake application FIRST (without DbContext)
            _micakeApp = services.AddMiCake<TestModule>()
                    .UseAudit()
                    .UseEFCore<TestDbContext>()
                    .Build();

            // Build initial ServiceProvider
            ServiceProvider = services.BuildServiceProvider();

            var micakeApp = ServiceProvider.GetService<IMiCakeApplication>();
            if (micakeApp is IDependencyReceiver<IServiceProvider> needServiceProvider)
            {
                needServiceProvider.AddDependency(ServiceProvider);
            }

            micakeApp?.Start().GetAwaiter().GetResult();

            // Now add DbContext AFTER framework initialization
            services.AddDbContext<TestDbContext>(options =>
                options.UseSqlite(_connection));

            // CRITICAL FIX: Reset the interceptor factory static state BEFORE building the second provider.
            // This ensures the new provider's singleton LazyEFSaveChangesLifetime will be properly registered
            // with the new provider's IServiceScopeFactory, instead of reusing the old one from the first provider.
            MiCakeInterceptorFactoryHelper.Reset();

            // Rebuild ServiceProvider with DbContext
            ServiceProvider = services.BuildServiceProvider();

            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                dbContext.Database.EnsureCreated();
            }

            // Get UnitOfWorkManager
            UowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            // Clear event handlers
            Fakes.ProductPriceChangedHandler.Clear();
            Fakes.ProductStockDecreasedHandler.Clear();
            Fakes.OrderCompletedHandler.Clear();
        }

        protected T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        protected TestDbContext GetDbContext()
        {
            return GetService<TestDbContext>();
        }

        public virtual void Dispose()
        {
            _micakeApp?.ShutDown();
            _connection?.Close();
            _connection?.Dispose();
            (ServiceProvider as IDisposable)?.Dispose();
        }
    }
}
