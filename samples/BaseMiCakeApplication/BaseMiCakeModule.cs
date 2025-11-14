using MiCake.AspNetCore.Modules;
using MiCake.Core.Modularity;

namespace BaseMiCakeApplication
{
    [RelyOn(typeof(MiCakeAspNetCoreModule))]
    public class BaseMiCakeModule : MiCakeModule
    {
        public override void ConfigureServices(ModuleConfigServiceContext context)
        {
            context.AutoRegisterRepositories(typeof(BaseMiCakeModule).Assembly);
            base.ConfigureServices(context);
        }
    }
}
