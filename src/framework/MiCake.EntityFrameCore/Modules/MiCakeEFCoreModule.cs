using MiCake.Core.Modularity;
using MiCake.DDD.Domain.Modules;
using MiCake.DDD.Extensions.Modules;
using MiCake.EntityFrameworkCore.Diagnostics;
using MiCake.Uow.Modules;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MiCake.EntityFrameworkCore.Modules
{
    [DependOn(
        typeof(MiCakeUowModule),
        typeof(MiCakeDDDExtensionsModule),
        typeof(MiCakeDomainModule))]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeEFCoreModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            services.AddScoped(typeof(SaveChangesInterceptor));
        }

        public override void Initialization(ModuleBearingContext context)
        {
            DiagnosticListener.AllListeners.Subscribe(new EfGlobalListener(context.ServiceProvider));
        }
    }
}
