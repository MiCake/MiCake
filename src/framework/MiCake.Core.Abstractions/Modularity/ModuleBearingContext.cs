using System;

namespace MiCake.Core.Modularity
{
    public class ModuleBearingContext
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public IMiCakeModuleCollection MiCakeModules { get; private set; }

        public ModuleBearingContext(IServiceProvider serviceProvider, IMiCakeModuleCollection miCakeModules)
        {
            ServiceProvider = serviceProvider;

            MiCakeModules = miCakeModules;
        }
    }
}
