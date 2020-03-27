using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.DDD.Extensions.Modules;
using MiCake.Uow.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore.Modules
{
    [DependOn(typeof(MiCakeDDDExtensionsModule), typeof(MiCakeUowModule))]
    public class MiCakeAspNetCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeAspNetCoreModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.AddSingleton<IConfigureOptions<MvcOptions>, MvcOptionsConfigure>();
        }

        public override void PostInitialization(ModuleBearingContext context)
        {
            context.ServiceProvider.GetService(typeof(IServiceLocator));
        }
    }
}
