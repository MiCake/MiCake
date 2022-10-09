namespace MiCake.Core.Modularity
{
    public class ModuleLoadContext
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IMiCakeModuleCollection MiCakeModules { get; private set; }
        public IMiCakeApplication MiCakeApplication { get; private set; }

        public ModuleLoadContext(
            IServiceProvider serviceProvider,
            IMiCakeModuleCollection miCakeModules,
            IMiCakeApplication application
            )
        {
            ServiceProvider = serviceProvider;
            MiCakeModules = miCakeModules;
            MiCakeApplication = application;
        }
    }
}
