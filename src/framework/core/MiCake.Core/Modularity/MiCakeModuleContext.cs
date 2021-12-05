namespace MiCake.Core.Modularity
{
    public class MiCakeModuleContext : IMiCakeModuleContext
    {
        public IMiCakeModuleCollection AllModules { get; private set; }

        public IMiCakeModuleCollection MiCakeModules { get; private set; }

        public IMiCakeModuleCollection FeatureModules { get; private set; }

        public MiCakeModuleContext()
        {
            AllModules = new MiCakeModuleCollection();
            MiCakeModules = new MiCakeModuleCollection();
            FeatureModules = new MiCakeModuleCollection();
        }

        public MiCakeModuleContext(
            IMiCakeModuleCollection allModules,
            IMiCakeModuleCollection normalModules,
            IMiCakeModuleCollection featureModules)
        {
            AllModules = allModules;
            MiCakeModules = normalModules;
            FeatureModules = featureModules;
        }
    }
}
