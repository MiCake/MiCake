using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.Cord.Modules;
using MiCake.Core.Modularity;
using MiCake.Identity.Modules;
using MiCake.Uow.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore.Modules
{
    [RelyOn(typeof(MiCakeDDDModule), typeof(MiCakeUowModule), typeof(MiCakeIdentityModule))]
    [CoreModule]
    public class MiCakeAspNetCoreModule : MiCakeModule
    {
        public MiCakeAspNetCoreModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.AddSingleton<IDataWrapperExecutor, DefaultWrapperExecutor>();
            services.AddSingleton<IConfigureOptions<MvcOptions>, MvcOptionsConfigure>();
        }
    }
}
