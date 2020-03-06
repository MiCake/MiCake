using BaseMiCakeApplication.Domain.Repositories;
using BaseMiCakeApplication.EFCore.Repositories;
using MiCake;
using MiCake.AspNetCore.Modules;
using MiCake.Core.Modularity;

namespace BaseMiCakeApplication
{
    [DependOn(typeof(MiCakeAspNetCoreModule))]
    public class BaseMiCakeModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.RegisterRepository<IItineraryRepository, ItineraryRepository>();
        }
    }
}
