using MiCake.Audit.Modules;
using MiCake.Core.Modularity;

namespace MiCake.Identity.Modules
{
    [RelyOn(typeof(MiCakeAuditModule))]
    public class MiCakeIdentityModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
        }
    }
}
