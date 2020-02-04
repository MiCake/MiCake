using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Audit.Modules
{
    public class MiCakeAuditModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            services.AddScoped<IAuditContext, AuditContext>();
        }
    }
}
