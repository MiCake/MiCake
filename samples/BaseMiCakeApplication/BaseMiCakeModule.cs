using MiCake.Core.Modularity;

namespace BaseMiCakeApplication
{
    public class BaseMiCakeModule : MiCakeModule
    {
        public override void ConfigureServices(ModuleConfigServiceContext context)
        {
            context.AutoRegisterRepositories(typeof(BaseMiCakeModule).Assembly);
            base.ConfigureServices(context);
        }
    }
}
