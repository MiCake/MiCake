using MiCake.Cord.Lifetime;
using MiCake.Cord.Storage;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Cord.Modules;

[CoreModule]
public class MiCakeDDDModule : MiCakeModule
{
    public StoreConfig DomainModelStoreConfig { get; } = new StoreConfig();

    public override void PreConfigServices(ModuleConfigServiceContext context)
    {
        var services = context.Services;

        //regiter all domain event handler to services
        services.ResigterDomainEventHandler(context.MiCakeModules);

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        //LifeTime
        services.AddTransient<IRepositoryPreSaveChanges, DomainEventsRepositoryLifetime>();
    }
}
