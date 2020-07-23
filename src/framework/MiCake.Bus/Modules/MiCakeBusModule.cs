using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Bus.Modules
{
    public class MiCakeBusModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.TryAddSingleton<IBus, DefaultBus>();
            services.TryAddSingleton<IBusConsumer, DefaultBusConsumer>();
        }
    }
}
