using MiCake.Audit.Modules;
using MiCake.Core.Modularity;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Modules;
using MiCake.EntityFrameworkCore.Diagnostics;
using MiCake.Uow.Modules;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MiCake.EntityFrameworkCore.Modules
{
    [DependOn(
        typeof(MiCakeUowModule),
        typeof(MiCakeAuditModule),
        typeof(MiCakeDDDExtensionsModule))]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeEFCoreModule()
        {
        }

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
            var services = context.Services;
            //add ef core interceptor
            services.AddScoped(typeof(SaveChangesInterceptor));

            //add ef repository provider
            services.AddScoped(typeof(IRepositoryProvider<,>), typeof(EFRepositoryProvider<,>));
        }

        public override void Initialization(ModuleBearingContext context)
        {
            DiagnosticListener.AllListeners.Subscribe(new EfGlobalListener(context.ServiceProvider));
        }
    }
}
