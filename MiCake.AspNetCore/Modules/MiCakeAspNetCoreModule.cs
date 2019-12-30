using MiCake.Autofac;
using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Serilog;

namespace MiCake.AspNetCore.Modules
{
    public class MiCakeAspNetCoreModule : MiCakeModule
    {
        public MiCakeAspNetCoreModule()
        {
        }

        public override void PostModuleInitialization(ModuleBearingContext context)
        {
            base.PostModuleInitialization(context);
            context.ServiceProvider.GetService(typeof(IServiceLocator ));
        }
    }
}
