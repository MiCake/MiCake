using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// The module manager for MiCake application.
    /// </summary>
    public interface IMiCakeModuleManager
    {
        /// <summary>
        /// Gets the module context (read-only access to loaded modules)
        /// </summary>
        IMiCakeModuleContext ModuleContext { get; }

        /// <summary>
        /// Indicates whether the module has been populated
        /// </summary>
        bool IsPopulated { get; }

        /// <summary>
        /// Add <see cref="IMiCakeModule"/> to collection.
        /// It's will be populated when call <see cref="PopulateModules(Type)"/>
        /// </summary>
        /// <param name="moduleType"><see cref="IMiCakeModule"/> to be added</param>
        void AddMiCakeModule(Type moduleType);

        /// <summary>
        /// Populate modules.
        /// </summary>
        /// <param name="entryType"></param>
        void PopulateModules(Type entryType);

        /// <summary>
        /// Get a MiCake module info from manager.
        /// </summary>
        /// <param name="moduleType">MiCake module type</param>
        /// <returns><see cref="MiCakeModuleDescriptor"/></returns>
        MiCakeModuleDescriptor GetMiCakeModule(Type moduleType);
    }
}
