using MiCake.Core.Abstractions.Modularity;
using MiCake.DDD.Domain.EventDispatch;
using Microsoft.Extensions.DependencyInjection;
using MiCake.DDD.Domain.Internel;

namespace MiCake.DDD.Domain.Modules
{
    public class MiCakeDomainModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => false;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            var moudules = context.MiCakeModules;

            //regiter all domain event handler to services
            services.ResigterDomainEventHandler(moudules);

            services.AddSingleton<IEventDispatcher, EventDispatcher>();
        }
    }
}
