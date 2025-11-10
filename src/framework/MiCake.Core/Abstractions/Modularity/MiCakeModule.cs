namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Base class for MiCake modules - Provides default implementation
    /// All MiCake modules should inherit from this base class.
    /// For fine-grained lifecycle control, inherit from <see cref="MiCakeModuleAdvanced"/>.
    /// </summary>
    public abstract class MiCakeModule : IMiCakeModule
    {
        /// <summary>
        /// Indicates whether this module is a framework-level module.
        /// Framework level modules do not need users to explicitly declare dependencies.
        /// </summary>
        public virtual bool IsFrameworkLevel => false;

        /// <summary>
        /// Indicates whether to automatically register services (via InjectServiceAttribute or marker interfaces).
        /// When true, services implementing ITransientService, IScopedService, or ISingletonService will be auto-registered.
        /// </summary>
        public virtual bool IsAutoRegisterServices => true;

        /// <summary>
        /// Module description
        /// </summary>
        public virtual string Description => string.Empty;

        /// <summary>
        /// Configure services - Register services to DI container during application startup
        /// This is the main place to register your services.
        /// </summary>
        /// <param name="context">Module configuration context containing Services, other modules, and application options</param>
        public virtual void ConfigureServices(ModuleConfigServiceContext context)
        {
            // Default implementation - no services to configure
        }

        /// <summary>
        /// Application initialization - Execute after application startup is complete
        /// Use this to perform initialization that requires services from the container.
        /// </summary>
        /// <param name="context">Module initialization context containing ServiceProvider, other modules, and application options</param>
        public virtual void OnApplicationInitialization(ModuleInitializationContext context)
        {
            // Default implementation - no initialization needed
        }

        /// <summary>
        /// Application shutdown - Execute cleanup work when application shuts down
        /// Use this to release resources and perform cleanup.
        /// </summary>
        /// <param name="context">Module shutdown context containing ServiceProvider, other modules, and application options</param>
        public virtual void OnApplicationShutdown(ModuleShutdownContext context)
        {
            // Default implementation - no cleanup needed
        }
    }
}
