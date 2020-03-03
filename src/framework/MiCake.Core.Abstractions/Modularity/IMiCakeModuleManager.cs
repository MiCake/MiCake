using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Manages the modules of an Micake application.
    /// </summary>
    public interface IMiCakeModuleManager
    {
        /// <summary>
        /// Include all modules info <see cref="IMiCakeModuleContext"/>
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
        void AddMiCakeModule();

        /// <summary>
        /// Populate modules.
        /// </summary>
        /// <param name="entryType"></param>
        void PopulateModules(Type entryType);

        /// <summary>
        /// Get a micake module info from manager.
        /// </summary>
        /// <param name="moduleType">micake module type</param>
        /// <returns><see cref="MiCakeModuleDescriptor"/></returns>
        MiCakeModuleDescriptor GetMiCakeModule(Type moduleType);

        /// <summary>
        /// Add third-party extension module for micake
        /// </summary>
        void AddFeatureModule(Type featureModule);
    }
}
