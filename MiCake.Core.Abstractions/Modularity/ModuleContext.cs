using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    public class ModuleContext
    {
        public IServiceCollection Services { get; set; }

        public IMiCakeModuleCollection Modules { get; set; }
    }
}
