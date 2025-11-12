using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.EntityFrameworkCore.Modules
{
    /// <summary>
    /// MiCake Entity Framework Core module - Provides EF Core integration for MiCake framework
    /// </summary>
    [RelyOn(typeof(MiCakeEssentialModule))]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        /// <summary>
        /// Indicates this is a framework-level module
        /// </summary>
        public override bool IsFrameworkLevel => true;

        /// <summary>
        /// Configure services for EF Core module
        /// </summary>
        public override void ConfigureServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            // Register the lazy singleton version of IEFSaveChangesLifetime
            // This allows the interceptor to work even when created at application startup
            services.TryAddSingleton<IEFSaveChangesLifetime, LazyEFSaveChangesLifetime>();
            
            // Register the interceptor factory
            services.TryAddTransient<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();
        }

        /// <summary>
        /// Initialize the EF Core module
        /// </summary>
        public override void OnApplicationInitialization(ModuleInitializationContext context)
        {
            // Configure the interceptor factory helper with the factory from DI container
            // This ensures proper dependency injection without using ServiceLocator anti-pattern
            var factory = context.ServiceProvider.GetService<IMiCakeInterceptorFactory>();
            if (factory != null)
            {
                MiCakeInterceptorFactoryHelper.Configure(factory);
            }
        }
    }
}
