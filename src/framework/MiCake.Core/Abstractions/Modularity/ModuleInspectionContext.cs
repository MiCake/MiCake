using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// The context of module inspection.
    /// </summary>
    public class ModuleInspectionContext(IServiceProvider serviceProvider, IMiCakeModuleCollection miCakeModules)
    {
        /// <summary>
        /// <see cref="IServiceProvider"/>
        /// </summary>
        public IServiceProvider AppServiceProvider { get; } = serviceProvider;

        /// <summary>
        /// All loaded micake modules.
        /// </summary>
        public IMiCakeModuleCollection MiCakeModules { get; } = miCakeModules;
    }
}
