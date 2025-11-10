namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Advanced base class for MiCake modules with fine-grained lifecycle control
    /// Use this when you need precise control over module configuration and initialization order.
    /// Most modules should use <see cref="MiCakeModule"/> instead.
    /// </summary>
    public abstract class MiCakeModuleAdvanced : MiCakeModule, IMiCakeModuleAdvanced
    {
        /// <summary>
        /// Pre-configure services - Execute before ConfigureServices
        /// Use this for early configuration that other modules might depend on.
        /// </summary>
        /// <param name="context">Module configuration context containing Services, other modules, and application options</param>
        public virtual void PreConfigureServices(ModuleConfigServiceContext context)
        {
            // Default implementation - no pre-configuration needed
        }

        /// <summary>
        /// Post-configure services - Execute after ConfigureServices
        /// Use this for configuration that depends on services registered by other modules.
        /// </summary>
        /// <param name="context">Module configuration context containing Services, other modules, and application options</param>
        public virtual void PostConfigureServices(ModuleConfigServiceContext context)
        {
            // Default implementation - no post-configuration needed
        }

        /// <summary>
        /// Pre-initialization - Execute before OnApplicationInitialization
        /// </summary>
        /// <param name="context">Module initialization context containing ServiceProvider, other modules, and application options</param>
        public virtual void PreInitialization(ModuleInitializationContext context)
        {
            // Default implementation - no pre-initialization needed
        }

        /// <summary>
        /// Post-initialization - Execute after OnApplicationInitialization
        /// </summary>
        /// <param name="context">Module initialization context containing ServiceProvider, other modules, and application options</param>
        public virtual void PostInitialization(ModuleInitializationContext context)
        {
            // Default implementation - no post-initialization needed
        }

        /// <summary>
        /// Pre-shutdown - Execute before OnApplicationShutdown
        /// Use this to prepare for graceful shutdown.
        /// </summary>
        /// <param name="context">Module shutdown context containing ServiceProvider, other modules, and application options</param>
        public virtual void PreShutdown(ModuleShutdownContext context)
        {
            // Default implementation - no pre-shutdown needed
        }
    }
}
