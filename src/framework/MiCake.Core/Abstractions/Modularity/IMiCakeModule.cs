using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// MiCake module interface - Simplified 3-method lifecycle
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
        bool IsAutoRegisterServices { get; }

        /// <summary>
        /// Module description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Configure services - Register services to DI container during application startup
        /// </summary>
        /// <param name="context">Contains Services, other modules, and application options</param>
        Task ConfigureServices(object context);

        /// <summary>
        /// Application initialization - Execute after application startup is complete
        /// </summary>
        /// <param name="context">Contains ServiceProvider, other modules, and application options</param>
        Task OnApplicationInitialization(object context);

        /// <summary>
        /// Application shutdown - Execute cleanup work when application shuts down
        /// </summary>
        /// <param name="context">Contains ServiceProvider, other modules, and application options</param>
        Task OnApplicationShutdown(object context);
    }
}
