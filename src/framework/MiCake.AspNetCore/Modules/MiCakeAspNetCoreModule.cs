using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Extensions.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore.Modules
{
    [DependOn(typeof(MiCakeEFCoreExtensionsModule))]
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

        public override void PostModuleInitialization(ModuleBearingContext context)
        {
            base.PostModuleInitialization(context);
            context.ServiceProvider.GetService(typeof(IServiceLocator));
        }
    }
}
