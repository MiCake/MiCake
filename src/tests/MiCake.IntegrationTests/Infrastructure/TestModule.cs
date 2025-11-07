using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Modules;
using MiCake.Modules;

namespace MiCake.IntegrationTests.Infrastructure
{
    [RelyOn(typeof(MiCakeEFCoreModule))]
    [RelyOn(typeof(MiCakeEssentialModule))]
    public class TestModule : MiCakeModule
    {
        public override Task PostConfigServices(ModuleConfigServiceContext context)
        {
            return base.PostConfigServices(context);
        }
    }
}
