using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Base class for MiCake modules.
    /// Modules are the building blocks of MiCake applications and follow a lifecycle pattern.
    /// </summary>
    public abstract class MiCakeModule : IMiCakeModule
    {
        /// <summary>
        /// Indicates whether this module is a framework-level module.
        /// Framework level modules do not need to be traversed during auto-registration.
        /// </summary>
        public virtual bool IsFrameworkLevel => false;

        /// <summary>
        /// Indicates whether services should be auto-registered to the dependency injection framework.
        /// Services implementing ITransientService, IScopedService, or ISingletonService will be automatically registered.
        /// </summary>
        public virtual bool IsAutoRegisterServices => true;

        public MiCakeModule()
        {
        }

        /// <summary>
        /// Configure services in the dependency injection container.
        /// This is the main place to register your services.
        /// </summary>
        /// <param name="context">The module configuration context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task ConfigServices(ModuleConfigServiceContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initialize the module after all services have been configured.
        /// Use this to perform initialization that requires services from the container.
        /// </summary>
        /// <param name="context">The module load context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task Initialization(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hook executed after ConfigServices for all modules.
        /// Use this for configuration that depends on services registered by other modules.
        /// </summary>
        /// <param name="context">The module configuration context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PostConfigServices(ModuleConfigServiceContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hook executed after Initialization for all modules.
        /// </summary>
        /// <param name="context">The module load context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PostInitialization(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hook executed before ConfigServices for all modules.
        /// Use this for early configuration that other modules might depend on.
        /// </summary>
        /// <param name="context">The module configuration context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PreConfigServices(ModuleConfigServiceContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hook executed before Initialization for all modules.
        /// </summary>
        /// <param name="context">The module load context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PreInitialization(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hook executed before application shutdown.
        /// Use this to prepare for graceful shutdown.
        /// </summary>
        /// <param name="context">The module load context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task PreShutDown(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the application is shutting down.
        /// Use this to release resources and perform cleanup.
        /// </summary>
        /// <param name="context">The module load context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual Task Shutdown(ModuleLoadContext context)
        {
            return Task.CompletedTask;
        }
    }
}
