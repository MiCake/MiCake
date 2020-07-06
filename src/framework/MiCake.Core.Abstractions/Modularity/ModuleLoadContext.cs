using System;

namespace MiCake.Core.Modularity
{
    public class ModuleLoadContext
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IMiCakeModuleCollection MiCakeModules { get; private set; }
        public MiCakeApplicationOptions MiCakeApplicationOptions { get; set; }

        public ModuleLoadContext(
            IServiceProvider serviceProvider,
            IMiCakeModuleCollection miCakeModules,
            MiCakeApplicationOptions applicationOptions
            )
        {
            ServiceProvider = serviceProvider;
            MiCakeModules = miCakeModules;
            MiCakeApplicationOptions = applicationOptions;
        }
    }
}
