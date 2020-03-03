using System;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Manages the modules of an Micake application.
    /// </summary>
    public interface IMiCakeModuleManager
    {
        /// <summary>
        /// Include MiCakeModules and FeatureModules.
        /// </summary>
        IMiCakeModuleCollection AllModules { get; }

        IMiCakeModuleCollection MiCakeModules { get; }

        IMiCakeModuleCollection FeatureModules { get; }

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
