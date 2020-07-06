using MiCake.Core.Modularity;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Domain.Modules
{
    public class MiCakeDomainModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            var moudules = context.MiCakeModules;

            //regiter all domain event handler to services
            services.ResigterDomainEventHandler(moudules);

            services.AddSingleton<IEventDispatcher, EventDispatcher>();
        }
    }
}
