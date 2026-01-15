using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Modules;
using MiCake.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        public override void PostConfigureServices(ModuleConfigServiceContext context)
        {
            base.PostConfigureServices(context);

            var services = context.Services;
            RegisterApiLoggingServices(services);   // put it to post configure to allow user override default implementations
        }

        private static void RegisterApiLoggingServices(IServiceCollection services)
        {
            // Register default implementations (can be replaced by user)
            services.TryAddSingleton<IApiLoggingConfigProvider, OptionsApiLoggingConfigProvider>();
            services.TryAddSingleton<IApiLogWriter, MicrosoftLoggerApiLogWriter>();
            services.TryAddSingleton<ISensitiveDataMasker, JsonSensitiveDataMasker>();
            services.TryAddSingleton<IApiLogEntryFactory, DefaultApiLogEntryFactory>();

            // Register built-in processors
            services.AddSingleton<IApiLogProcessor, SensitiveMaskProcessor>();
            services.AddSingleton<IApiLogProcessor, TruncationProcessor>();
        }
    }
}
