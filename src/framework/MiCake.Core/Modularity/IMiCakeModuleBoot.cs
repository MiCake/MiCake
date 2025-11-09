using System;
using System.Threading.Tasks;

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
        Task ConfigServices(ModuleConfigServiceContext context);

        /// <summary>
        /// Initialize all modules
        /// </summary>
        Task Initialization(ModuleInitializationContext context);

        /// <summary>
        /// Shutdown all modules
        /// </summary>
        Task ShutDown(ModuleShutdownContext context);

        /// <summary>
        /// Add other config service actions. 
        /// This part of the operation will be called at the end of ConfigServices().
        /// </summary>
        Task AddConfigService(Action<ModuleConfigServiceContext> configServiceAction);

        /// <summary>
        /// Add other initialization actions. 
        /// This part of the operation will be called at the end of Initialization().
        /// </summary>
        Task AddInitalzation(Action<ModuleInitializationContext> initalzationAction);
    }
}
