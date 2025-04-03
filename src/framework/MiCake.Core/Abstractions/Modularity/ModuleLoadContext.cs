using System;

namespace MiCake.Core.Modularity
{
    public class ModuleLoadContext(
        IServiceProvider serviceProvider,
        IMiCakeModuleCollection miCakeModules,
        MiCakeApplicationOptions applicationOptions
            )
    {
        public IServiceProvider ServiceProvider { get; private set; } = serviceProvider;
        public IMiCakeModuleCollection MiCakeModules { get; private set; } = miCakeModules;
        public MiCakeApplicationOptions MiCakeApplicationOptions { get; set; } = applicationOptions;
    }
}
