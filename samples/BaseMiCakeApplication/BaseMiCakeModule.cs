using MiCake.Core.Modularity;

namespace BaseMiCakeApplication
{
    public class BaseMiCakeModule : MiCakeModule
    {
        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.AutoRegisterRepositories(typeof(BaseMiCakeModule).Assembly);
        }
    }
}
