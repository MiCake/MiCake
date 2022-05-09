namespace MiCake.Core.Modularity
{
    public class ModuleConfigServiceContext
    {
        public IServiceCollection Services { get; private set; }
        public IMiCakeModuleCollection MiCakeModules { get; private set; }
        public IMiCakeApplication MiCakeApplication { get; private set; }

        public ModuleConfigServiceContext(
            IServiceCollection services,
            IMiCakeModuleCollection miCakeModules,
            IMiCakeApplication application)
        {
            Services = services;
            MiCakeModules = miCakeModules;
            MiCakeApplication = application;
        }
    }
}
