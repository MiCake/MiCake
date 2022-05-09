using MiCake.Core.Modularity;
using MiCake.DDD.Connector;
using MiCake.DDD.Connector.Modules;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.Uow.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.EntityFrameworkCore.Modules
{
    [RelyOn(
        typeof(MiCakeUowModule),
        typeof(MiCakeDDDModule))]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeEFCoreModule()
        {
        }

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.TryAddTransient<IEFSaveChangesLifetime, DefaultEFSaveChangesLifetime>();
            //add ef repository provider
            services.AddScoped(typeof(IRepositoryProvider<,>), typeof(EFRepositoryProvider<,>));
        }

        public override void Initialization(ModuleLoadContext context)
        {
        }
    }
}
