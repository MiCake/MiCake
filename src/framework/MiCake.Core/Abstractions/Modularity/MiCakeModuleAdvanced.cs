using System.Threading.Tasks;

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
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PreConfigureServices(ModuleConfigServiceContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Post-configure services - Execute after ConfigureServices
        /// Use this for configuration that depends on services registered by other modules.
        /// </summary>
        /// <param name="context">Module configuration context containing Services, other modules, and application options</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PostConfigureServices(ModuleConfigServiceContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Pre-initialization - Execute before OnApplicationInitialization
        /// </summary>
        /// <param name="context">Module initialization context containing ServiceProvider, other modules, and application options</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PreInitialization(ModuleInitializationContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Post-initialization - Execute after OnApplicationInitialization
        /// </summary>
        /// <param name="context">Module initialization context containing ServiceProvider, other modules, and application options</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PostInitialization(ModuleInitializationContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Pre-shutdown - Execute before OnApplicationShutdown
        /// Use this to prepare for graceful shutdown.
        /// </summary>
        /// <param name="context">Module shutdown context containing ServiceProvider, other modules, and application options</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PreShutdown(ModuleShutdownContext context)
        {
            return Task.CompletedTask;
        }
    }
}
