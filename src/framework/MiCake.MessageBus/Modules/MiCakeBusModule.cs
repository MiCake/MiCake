using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.MessageBus.Modules
{
    public class MiCakeBusModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.TryAddSingleton<IMessageBus, DefaultMessageBus>();
        }
    }
}
