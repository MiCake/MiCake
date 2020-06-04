using MiCake.Audit.Modules;
using MiCake.Core.Modularity;
using MiCake.Identity.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Identity.Modules
{
    [RelyOn(typeof(MiCakeAuditModule))]
    public class MiCakeIdentityModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            services.TryAddSingleton<IJwtSupporter, JwtSupporter>();
        }
    }
}
