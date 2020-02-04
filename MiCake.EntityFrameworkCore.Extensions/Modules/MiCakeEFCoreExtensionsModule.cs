using MiCake.Audit.Modules;
using MiCake.Core.Abstractions.Modularity;
using MiCake.EntityFrameworkCore.Extensions.Audit;
using MiCake.EntityFrameworkCore.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.EntityFrameworkCore.Extensions.Modules
{
    [DependOn(typeof(MiCakeEFCoreModule),
        typeof(MiCakeAuditModule))]
    public class MiCakeEFCoreExtensionsModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;

            //add audit life time
            services.AddScoped<IEfRepositoryLifetime, AuditEFRepositoryLifetime>();
        }
    }
}
