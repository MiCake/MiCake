using MiCake.Core.Modularity;

namespace MiCake.Core.Tests.Modularity.Fakes
{
    [DependOn(typeof(DepencyModuleA))]
    public class DepencyModuleB : MiCakeModule
    {
    }
}
