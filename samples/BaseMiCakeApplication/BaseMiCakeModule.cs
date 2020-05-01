using BaseMiCakeApplication.Domain.Repositories;
using BaseMiCakeApplication.EFCore.Repositories;
using MiCake;
using MiCake.Core.Modularity;

namespace BaseMiCakeApplication
{
    public class BaseMiCakeModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.RegisterRepository<IItineraryRepository, ItineraryRepository>();
        }
    }
}
