using MiCake.AspNetCore.Modules;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace BaseMiCakeApplication
{
    /// <summary>
    /// BaseMiCakeApplication entry module.
    /// Configures application services and registers domain repositories.
    /// </summary>
    [RelyOn(typeof(MiCakeAspNetCoreModule))]
    public class BaseMiCakeModule : MiCakeModule
    {
        /// <summary>
        /// Configures services for the application.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Auto-registers repositories from the current assembly
        /// 2. Registers application services
        /// 3. Configures application-specific options
        /// </remarks>
        public override void ConfigureServices(ModuleConfigServiceContext context)
        {
            // Auto-register repositories for all aggregates in this assembly
            context.AutoRegisterRepositories(typeof(BaseMiCakeModule).Assembly);

            // Register application services
            RegisterApplicationServices(context.Services);

            base.ConfigureServices(context);
        }

        /// <summary>
        /// Registers application-specific services.
        /// </summary>
        private static void RegisterApplicationServices(IServiceCollection services)
        {
            // Application services can be registered here
            // Example: services.AddScoped<IBookService, BookService>();
        }
    }
}
