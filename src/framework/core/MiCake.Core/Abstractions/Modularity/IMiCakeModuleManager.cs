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
        /// Populate modules.
        /// </summary>
        /// <param name="entryType"></param>
        /// <param name="customSorter"><see cref="IMiCakeModuleSorter"/></param>
        void PopulateModules(Type entryType, IMiCakeModuleSorter? customSorter = null);

        /// <summary>
        /// Get a micake module info from manager.
        /// </summary>
        /// <param name="moduleType">micake module type</param>
        /// <returns><see cref="MiCakeModuleDescriptor"/></returns>
        MiCakeModuleDescriptor? GetMiCakeModule(Type moduleType);
    }
}
