using MiCake.Audit.Core;
using MiCake.Audit.SoftDeletion;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Audit.Modules
{
    public class MiCakeAuditModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            //Audit Executor
            services.AddScoped<IAuditExecutor, DefaultAuditExecutor>();
            //Audit CreationTime and ModifationTime
            services.AddScoped<IAuditProvider, DefaultTimeAuditProvider>();
            //Audit Deletion Time
            services.AddScoped<IAuditProvider, SoftDeletionAuditProvider>();
        }
    }
}
