using MiCake.Core.Modularity;
using MiCake.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Uow.Modules
{
    public class MiCakeUowModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeUowModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            context.Services.AddScoped<ICurrentUnitOfWork, CurrentUnitOfWork>();
            context.Services.AddTransient<IUnitOfWork, UnitOfWork>();
        }

        public override void Initialization(ModuleLoadContext context)
        {
        }
    }
}
