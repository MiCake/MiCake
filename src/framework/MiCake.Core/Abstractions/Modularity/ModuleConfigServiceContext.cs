using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Context for module service configuration phase.
    /// </summary>
    public class ModuleConfigServiceContext(
        IServiceCollection services,
        IMiCakeModuleCollection miCakeModules,
        MiCakeApplicationOptions miCakeApplication)
    {
        /// <summary>
        /// Gets the service collection for registering services
        /// </summary>
        public IServiceCollection Services { get; private set; } = services;

        /// <summary>
        /// Gets the collection of all registered modules
        /// </summary>
        public IMiCakeModuleCollection MiCakeModules { get; private set; } = miCakeModules;

        /// <summary>
        /// Gets the application options
        /// </summary>
        public MiCakeApplicationOptions MiCakeApplicationOptions { get; private set; } = miCakeApplication;
    }
}
