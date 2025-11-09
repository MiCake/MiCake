using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Advanced module interface - For scenarios requiring fine-grained control (optional implementation)
    /// Most modules should use <see cref="IMiCakeModule"/> instead.
    /// Only implement this interface when you need precise control over configuration order.
    /// </summary>
    public interface IMiCakeModuleAdvanced : IMiCakeModule
    {
        /// <summary>
        /// Pre-configure services - Execute before ConfigureServices
        /// Use this for early configuration that other modules might depend on.
        /// </summary>
        Task PreConfigureServices(ModuleConfigServiceContext context);

        /// <summary>
        /// Post-configure services - Execute after ConfigureServices
        /// Use this for configuration that depends on services registered by other modules.
        /// </summary>
        Task PostConfigureServices(ModuleConfigServiceContext context);

        /// <summary>
        /// Pre-initialization - Execute before OnApplicationInitialization
        /// </summary>
        Task PreInitialization(ModuleInitializationContext context);

        /// <summary>
        /// Post-initialization - Execute after OnApplicationInitialization
        /// </summary>
        Task PostInitialization(ModuleInitializationContext context);

        /// <summary>
        /// Pre-shutdown - Execute before OnApplicationShutdown
        /// Use this to prepare for graceful shutdown.
        /// </summary>
        Task PreShutdown(ModuleShutdownContext context);
    }
}
