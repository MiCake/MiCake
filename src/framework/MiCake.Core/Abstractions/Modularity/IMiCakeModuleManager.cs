using System;
using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Manages the modules of an MiCake application.
    /// </summary>
    public interface IMiCakeModuleManager
    {
        /// <summary>
        /// Include all modules info.
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
        Task AddMiCakeModule(Type moduleType);

        /// <summary>
        /// Populate modules.
        /// </summary>
        /// <param name="entryType"></param>
        Task PopulateModules(Type entryType);

        /// <summary>
        /// Get a micake module info from manager.
        /// </summary>
        /// <param name="moduleType">micake module type</param>
        /// <returns><see cref="MiCakeModuleDescriptor"/></returns>
        MiCakeModuleDescriptor GetMiCakeModule(Type moduleType);
    }
}
