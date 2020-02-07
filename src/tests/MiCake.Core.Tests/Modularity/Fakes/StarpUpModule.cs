using MiCake.Core.Abstractions.Modularity;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Tests.Modularity.Fakes
{
    [DependOn(typeof(DepencyModuleA))]
    [DependOn(typeof(DepencyModuleB))]
    public class StarpUpModule : MiCakeModule
    {
        public StarpUpModule()
        {
        }
    }
}
