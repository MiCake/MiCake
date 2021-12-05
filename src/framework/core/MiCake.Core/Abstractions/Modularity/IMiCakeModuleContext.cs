namespace MiCake.Core.Modularity
{
    public interface IMiCakeModuleContext
    {
        /// <summary>
        /// Include MiCakeModules and FeatureModules.
        /// </summary>
        IMiCakeModuleCollection AllModules { get; }

        /// <summary>
        /// MiCake Module Collection for normal modules
        /// </summary>
        IMiCakeModuleCollection MiCakeModules { get; }

        /// <summary>
        /// MiCake Module Collection for feature modules
        /// </summary>
        IMiCakeModuleCollection FeatureModules { get; }
    }
}
