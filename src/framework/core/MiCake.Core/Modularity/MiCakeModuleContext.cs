namespace MiCake.Core.Modularity
{
    public class MiCakeModuleContext : IMiCakeModuleContext
    {
        public IMiCakeModuleCollection MiCakeModules { get; private set; }

        public MiCakeModuleContext()
        {
            MiCakeModules = new MiCakeModuleCollection();
        }

        public MiCakeModuleContext(IMiCakeModuleCollection allModules)
        {
            MiCakeModules = allModules;
        }
    }
}
