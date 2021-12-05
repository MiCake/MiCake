using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// The context of module inspection.
    /// </summary>
    public class ModuleInspectionContext
    {
        /// <summary>
        /// <see cref="IServiceProvider"/>
        /// </summary>
        public IServiceProvider AppServiceProvider { get; }

        /// <summary>
        /// All loaded micake modules.
        /// </summary>
        public IMiCakeModuleCollection MiCakeModules { get; }

        public ModuleInspectionContext(IServiceProvider serviceProvider, IMiCakeModuleCollection miCakeModules)
        {
            AppServiceProvider = serviceProvider;
            MiCakeModules = miCakeModules;
        }
    }
}
