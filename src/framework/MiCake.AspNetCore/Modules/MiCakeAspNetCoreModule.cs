using System.Threading.Tasks;
using MiCake.AspNetCore.Internal;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Modules;
using MiCake.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore.Modules
{
    [RelyOn(typeof(MiCakeEssentialModule), typeof(MiCakeEFCoreModule))]
    public class MiCakeAspNetCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeAspNetCoreModule()
        {
        }

        public override Task ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.AddSingleton<IConfigureOptions<MvcOptions>, MvcOptionsConfigure>();

            //This services is only use in asp net core middleware.
            services.AddScoped<IMiCakeCurrentRequestContext, MiCakeCurrentRequestContext>();

            return Task.CompletedTask;
        }
    }
}
