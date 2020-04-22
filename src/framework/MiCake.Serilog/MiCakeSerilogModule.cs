using MiCake.Core.Modularity;

namespace MiCake.Serilog
{
    public class MiCakeSerilogModule : MiCakeModule, IFeatureModule
    {
        public override bool IsFrameworkLevel => true;

        public FeatureModuleLoadOrder Order { get; set; }
        public bool AutoRegisted { get; set; }

        public MiCakeSerilogModule()
        {
        }

        public override void ConfigServices(ModuleConfigServiceContext context)
        {
        }

        public override void Initialization(ModuleBearingContext context)
        {
        }

        public override void PostConfigServices(ModuleConfigServiceContext context)
        {
        }

        public override void PostInitialization(ModuleBearingContext context)
        {
        }

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
        }

        public override void PreInitialization(ModuleBearingContext context)
        {
        }

        public override void PreShutDown(ModuleBearingContext context)
        {
        }

        public override void Shutdown(ModuleBearingContext context)
        {
        }
    }
}
