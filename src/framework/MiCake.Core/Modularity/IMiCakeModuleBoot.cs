using System;
using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Micake module boot.use to initialization module and shutdown module.
    /// </summary>
    internal interface IMiCakeModuleBoot
    {
        Task ConfigServices(ModuleConfigServiceContext context);

        Task Initialization(ModuleLoadContext context);

        Task ShutDown(ModuleLoadContext context);

        /// <summary>
        /// Add other configservice actions. 
        /// This part of the operation will be called at the end of ConfigServices().
        /// </summary>
        /// <param name="configServiceAction"></param>
        Task AddConfigService(Action<ModuleConfigServiceContext> configServiceAction);

        /// <summary>
        /// Add other initalzation actions. 
        /// This part of the operation will be called at the end of Initialization().
        /// </summary>
        /// <param name="initalzationAction"></param>
        Task AddInitalzation(Action<ModuleLoadContext> initalzationAction);
    }
}
