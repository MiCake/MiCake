using MiCake.Core.Modularity;

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
