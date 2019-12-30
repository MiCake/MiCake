using MiCake.Core.Abstractions.Modularity;
using MiCake.Uow.Modules;

namespace MiCake.EntityFrameCore.Modules
{
    [DependOn(typeof(MiCakeUowModule))]
    internal class MiCakeEFCoreModule : MiCakeModule
    {
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
