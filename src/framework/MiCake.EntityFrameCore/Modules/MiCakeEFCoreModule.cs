using MiCake.Core.Abstractions.Modularity;
using MiCake.Uow.Modules;

namespace MiCake.EntityFrameworkCore.Modules
{
    [DependOn(typeof(MiCakeUowModule))]
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
