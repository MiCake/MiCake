using MiCake.Core.Modularity;
using MiCake.DDD.Connector.Internal;
using MiCake.DDD.Connector.Lifetime;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Connector.Modules
{
    [CoreModule]
    public class MiCakeDDDModule : MiCakeModule
    {
        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            //regiter all domain event handler to services
            services.ResigterDomainEventHandler(context.MiCakeModules);

            services.AddScoped<IEventDispatcher, EventDispatcher>();

            services.AddScoped(typeof(IRepository<,>), typeof(ProxyRepository<,>));
            services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ProxyReadOnlyRepository<,>));
            services.AddScoped(typeof(IRepositoryFactory<,>), typeof(DefaultRepositoryFacotry<,>));

            //LifeTime
            services.AddTransient<IRepositoryPreSaveChanges, DomainEventsRepositoryLifetime>();
        }
    }
}
