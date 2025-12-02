using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;

namespace MiCake.Core
{
    /// <summary>
    /// MiCake root module - The fundamental module for all MiCake applications.
    /// This is a framework-level module that handles core functionality like service auto-registration.
    /// All user modules should depend on this module (directly or indirectly).
    /// </summary>
    public class MiCakeRootModule : MiCakeModuleAdvanced
    {
        /// <summary>
        /// This is a framework-level module
        /// </summary>
        public override bool IsFrameworkLevel => true;

        /// <summary>
        /// Enable auto-registration for this module
        /// </summary>
        public override bool EnableAutoServiceRegistration => true;

        /// <summary>
        /// Module description
        /// </summary>
        public override string Description => "MiCake Root Module - Core functionality for MiCake Framework";

        /// <summary>
        /// Configure core services
        /// </summary>
        public override void PreConfigureServices(ModuleConfigServiceContext context)
        {
            // Auto-register services from all modules
            AutoRegisterServices(context);
        }

        /// <summary>
        /// Auto-register services based on marker interfaces
        /// </summary>
        private static void AutoRegisterServices(ModuleConfigServiceContext context)
        {
            var serviceRegistrar = new DefaultServiceRegistrar(context.Services);
            
            // Use custom service type finder if provided
            if (context.MiCakeApplicationOptions.FindAutoServiceTypes != null)
            {
                serviceRegistrar.SetServiceTypesFinder(context.MiCakeApplicationOptions.FindAutoServiceTypes);
            }

            // Register services from all modules
            serviceRegistrar.Register(context.MiCakeModules);
        }
    }
}
