using MiCake.Core.Modularity;
using MiCake.Uow.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Uow.Modules
{
    [CoreModule]
    public class MiCakeUowModule : MiCakeModule
    {
        public MiCakeUowModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
            context.Services.TryAddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            context.Services.TryAddTransient<UnitOfWork, UnitOfWork>();
        }
    }
}
