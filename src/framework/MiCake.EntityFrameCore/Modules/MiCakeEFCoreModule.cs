using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain.Modules;
using MiCake.DDD.Extensions.Modules;
using MiCake.EntityFrameworkCore.Diagnostics;
using MiCake.Uow.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;
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

            var options = services.BuildServiceProvider().GetService<IObjectAccessor<MiCakeEFCoreOptions>>().Value ??
                                throw new NullReferenceException($"{nameof(MiCakeEFCoreOptions)} is null," +
                                $"Please check whether the EF configuration is used correctly,Add UseEFCore in your AddMiCake() action.");

            //register the repository
            var efCoreRepositoryRegister = new EFCoreRepositoryRegister(services, options);
            efCoreRepositoryRegister.Register(context.MiCakeModules, services);

            services.AddScoped(typeof(SaveChangesInterceptor));
        }

        public override void Initialization(ModuleBearingContext context)
        {
            DiagnosticListener.AllListeners.Subscribe(new EfGlobalListener(context.ServiceProvider));
        }
    }
}
