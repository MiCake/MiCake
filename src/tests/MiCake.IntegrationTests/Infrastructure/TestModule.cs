using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Modules;
using MiCake.Modules;
using Microsoft.Extensions.DependencyInjection;
using MiCake.IntegrationTests.Fakes;
using MiCake.DDD.Domain;

namespace MiCake.IntegrationTests.Infrastructure
{
    [RelyOn(typeof(MiCakeEFCoreModule))]
    [RelyOn(typeof(MiCakeEssentialModule))]
    public class TestModule : MiCakeModule
    {
        public override Task ConfigServices(ModuleConfigServiceContext context)
        {
            // Register event handlers as Singleton to match static collection lifetime
            // These will override the auto-registered Transient versions
            var services = context.Services;
            services.AddSingleton<ProductPriceChangedHandler>();
            services.AddSingleton<IDomainEventHandler<ProductPriceChangedEvent>>(sp => sp.GetRequiredService<ProductPriceChangedHandler>());
            
            services.AddSingleton<ProductStockDecreasedHandler>();
            services.AddSingleton<IDomainEventHandler<ProductStockDecreasedEvent>>(sp => sp.GetRequiredService<ProductStockDecreasedHandler>());
            
            services.AddSingleton<OrderCompletedHandler>();
            services.AddSingleton<IDomainEventHandler<OrderCompletedEvent>>(sp => sp.GetRequiredService<OrderCompletedHandler>());
            
            return base.ConfigServices(context);
        }
    }
}
