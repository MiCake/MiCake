namespace MiCake.Core.Modularity
{
    /// <summary>
    /// MiCake module interface
    /// This is the standard interface that all MiCake modules should implement.
    /// For more fine-grained control, implement <see cref="IMiCakeModuleAdvanced"/> instead.
    /// </summary>
    public interface IMiCakeModule
    {
        /// <summary>
        /// Indicates whether this module is a framework-level module.
        /// Framework level modules do not need users to explicitly declare dependencies.
        /// </summary>
        bool IsFrameworkLevel { get; }

        /// <summary>
        /// Indicates whether to automatically register services (via InjectServiceAttribute or marker interfaces).
        /// When true, services implementing ITransientService, IScopedService, or ISingletonService will be auto-registered.
        /// </summary>
        bool EnableAutoServiceRegistration { get; }

        /// <summary>
        /// Module description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Configure services - Register services to DI container during application startup.
        /// This method is called synchronously during the build phase.
        /// </summary>
        /// <param name="context">Module configuration context containing Services, other modules, and application options</param>
        void ConfigureServices(ModuleConfigServiceContext context);

        /// <summary>
        /// Application initialization - Execute after application startup is complete.
        /// This method is called synchronously when the application starts.
        /// </summary>
        /// <param name="context">Module initialization context containing ServiceProvider, other modules, and application options</param>
        void OnApplicationInitialization(ModuleInitializationContext context);

        /// <summary>
        /// Application shutdown - Execute cleanup work when application shuts down.
        /// This method is called synchronously during application shutdown.
        /// </summary>
        /// <param name="context">Module shutdown context containing ServiceProvider, other modules, and application options</param>
        void OnApplicationShutdown(ModuleShutdownContext context);
    }
}
