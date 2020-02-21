using MiCake.Core.Modularity;

namespace MiCake.Core.Tests.Modularity.Fakes
{
    [DependOn(typeof(DepencyModuleB))]
    public class DepencyModuleA : MiCakeModule
    {
    }
}
