namespace MiCake.Core.Tests.Modularity.Fakes
{
    [RelyOn(typeof(DepencyModuleA))]
    [RelyOn(typeof(DepencyModuleB))]
    public class StartUpModule : MiCakeModule
    {
        public StartUpModule()
        {
        }
    }
}
