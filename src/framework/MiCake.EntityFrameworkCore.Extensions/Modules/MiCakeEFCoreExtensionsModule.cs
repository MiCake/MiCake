using MiCake.Audit.Modules;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore.Extensions.Audit;
using MiCake.EntityFrameworkCore.Extensions.DDD;
using MiCake.EntityFrameworkCore.LifeTime;
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
            
        }
    }
}
