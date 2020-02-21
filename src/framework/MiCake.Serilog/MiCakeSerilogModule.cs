using MiCake.Core.ExceptionHandling;
using MiCake.Core.Logging;
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
            var micakeErrorHandler = GetServiceFromCollection<IMiCakeErrorHandler>(context.Services);
            var serilogHandlerProvide = GetServiceFromCollection<ILogErrorHandlerProvider>(context.Services);

            micakeErrorHandler?.ConfigureHandlerService(serilogHandlerProvide.GetErrorHandler());
        }

        public override void PostModuleInitialization(ModuleBearingContext context)
        {
        }

        public override void PreConfigServices(ModuleConfigServiceContext context)
        {
        }

        public override void PreModuleInitialization(ModuleBearingContext context)
        {
        }

        public override void PreModuleShutDown(ModuleBearingContext context)
        {
        }

        public override void Shuntdown(ModuleBearingContext context)
        {
        }
    }
}
