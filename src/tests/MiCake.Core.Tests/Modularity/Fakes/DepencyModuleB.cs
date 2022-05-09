namespace MiCake.Core.Tests.Modularity.Fakes
{
    [RelyOn(typeof(DepencyModuleA))]
    public class DepencyModuleB : MiCakeModule
    {
    }
}
