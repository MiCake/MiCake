using MiCake.Core.Modularity;
using MiCake.Uow.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            context.Services.TryAddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            context.Services.TryAddScoped<ICurrentUnitOfWork, CurrentUnitOfWork>();
            context.Services.TryAddTransient<IUnitOfWork, UnitOfWork>();
        }

        public override void Initialization(ModuleLoadContext context)
        {
        }
    }
}
