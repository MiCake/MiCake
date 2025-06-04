using System.Threading.Tasks;
using MiCake.Core.Modularity;
using MiCake.DDD.Extensions;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.EntityFrameworkCore.Modules
{
    [RelyOn(typeof(MiCakeEssentialModule))]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeEFCoreModule()
        {
        }

        public override Task PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.TryAddTransient<IEFSaveChangesLifetime, DefaultEFSaveChangesLifetime>();
            //add ef repository provider
            services.AddScoped(typeof(IRepositoryProvider<,>), typeof(EFRepositoryProvider<,>));

            return Task.CompletedTask;
        }
    }
}
