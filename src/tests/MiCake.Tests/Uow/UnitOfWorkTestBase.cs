using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Uow.Tests
{
    public abstract class UnitOfWorkTestBase
    {
        protected virtual IServiceProvider GetServiceProvider(Action<IServiceCollection> otherServices = null)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddOptions<UnitOfWorkOptions>();
            services.AddLogging();

            otherServices?.Invoke(services);

            return services.BuildServiceProvider();
        }
    }
}