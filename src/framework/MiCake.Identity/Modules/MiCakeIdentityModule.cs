using MiCake.Core.Modularity;

namespace MiCake.Identity.Modules
{
    public class MiCakeIdentityModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
        }
    }
}
