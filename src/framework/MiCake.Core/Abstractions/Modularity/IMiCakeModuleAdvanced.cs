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
        void PreConfigureServices(ModuleConfigServiceContext context);

        /// <summary>
        /// Post-configure services - Execute after ConfigureServices
        /// Use this for configuration that depends on services registered by other modules.
        /// </summary>
        void PostConfigureServices(ModuleConfigServiceContext context);

        /// <summary>
        /// Pre-initialization - Execute before OnApplicationInitialization
        /// </summary>
        void PreInitialization(ModuleInitializationContext context);

        /// <summary>
        /// Post-initialization - Execute after OnApplicationInitialization
        /// </summary>
        void PostInitialization(ModuleInitializationContext context);

        /// <summary>
        /// Pre-shutdown - Execute before OnApplicationShutdown
        /// Use this to prepare for graceful shutdown.
        /// </summary>
        void PreShutdown(ModuleShutdownContext context);
    }
}
