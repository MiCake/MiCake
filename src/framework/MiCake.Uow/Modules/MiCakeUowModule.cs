using MiCake.Core.Modularity;
using MiCake.Uow.Options;
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
            //todo : if iunitofworkmanager life is singleton,Internal context will be in conflict.
            context.Services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            context.Services.AddTransient<IUnitOfWork, UnitOfWork>();

            var defaultUowOptions = new UnitOfWorkDefaultOptions() { Limit = UnitOfWorkLimit.Required };
            context.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(defaultUowOptions));
        }

        public override void Initialization(ModuleBearingContext context)
        {
        }
    }
}
