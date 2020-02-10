using MiCake.Core.Abstractions.Modularity;
using MiCake.DDD.Domain.Modules;
using MiCake.Uow.Modules;

namespace MiCake.EntityFrameworkCore.Modules
{
    [DependOn(
        typeof(MiCakeUowModule),
        typeof(MiCakeDomainModule))]
    public class MiCakeEFCoreModule : MiCakeModule
    {
        public override bool IsFrameworkLevel => true;

        public MiCakeEFCoreModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
        }

        public override void Initialization(ModuleBearingContext context)
        {
        }
    }
}
