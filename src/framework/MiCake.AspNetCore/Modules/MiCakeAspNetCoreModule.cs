using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.AspNetCore.Internal;
using MiCake.Core.Modularity;
using MiCake.DDD.Extensions.Modules;
using MiCake.Uow.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore.Modules
{
    [RelyOn(typeof(MiCakeDDDExtensionsModule), typeof(MiCakeUowModule))]
    public class MiCakeAspNetCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeAspNetCoreModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.AddSingleton<IDataWrapperExecutor, DefaultWrapperExecutor>();
            services.AddSingleton<IConfigureOptions<MvcOptions>, MvcOptionsConfigure>();

            //This services is only use in asp net core middleware.
            services.AddScoped<IMiCakeCurrentRequestContext, MiCakeCurrentRequestContext>();
        }
    }
}
