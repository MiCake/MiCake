using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
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
