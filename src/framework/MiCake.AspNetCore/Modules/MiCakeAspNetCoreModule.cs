using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Modules;
using MiCake.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore.Modules
{
    /// <summary>
    /// MiCake ASP.NET Core module - Provides integration between MiCake framework and ASP.NET Core.
    /// This module registers filters for data wrapping and unit of work management.
    /// </summary>
    [RelyOn(typeof(MiCakeEFCoreModule), typeof(MiCakeEssentialModule))]
    public class MiCakeAspNetCoreModule : MiCakeModuleAdvanced
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override bool IsFrameworkLevel => true;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void PreConfigureServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            // Register MVC options configurator to add filters
            services.AddSingleton<IConfigureOptions<MvcOptions>, MvcOptionsConfigure>();
        }
    }
}
