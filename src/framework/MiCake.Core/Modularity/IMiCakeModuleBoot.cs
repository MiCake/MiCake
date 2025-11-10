using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// MiCake module boot interface - Handles module lifecycle operations
    /// </summary>
    internal interface IMiCakeModuleBoot
    {
        /// <summary>
        /// Configure services for all modules
        /// </summary>
        void ConfigServices(ModuleConfigServiceContext context);

        /// <summary>
        /// Initialize all modules
        /// </summary>
        void Initialization(ModuleInitializationContext context);

        /// <summary>
        /// Shutdown all modules
        /// </summary>
        void ShutDown(ModuleShutdownContext context);

        /// <summary>
        /// Add other config service actions. 
        /// This part of the operation will be called at the end of ConfigServices().
        /// </summary>
        void AddConfigService(Action<ModuleConfigServiceContext> configServiceAction);

        /// <summary>
        /// Add other initialization actions. 
        /// This part of the operation will be called at the end of Initialization().
        /// </summary>
        void AddInitalzation(Action<ModuleInitializationContext> initalzationAction);
    }
}
