using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    public struct ModuleBearingContext
    {
        public IServiceProvider ServiceProvider { get; }

        public IMiCakeModuleCollection MiCakeModules { get; }

        public ModuleBearingContext(IServiceProvider serviceProvider,IMiCakeModuleCollection miCakeModules)
        {
            ServiceProvider = serviceProvider;

            MiCakeModules = miCakeModules;
        }
    }
}
