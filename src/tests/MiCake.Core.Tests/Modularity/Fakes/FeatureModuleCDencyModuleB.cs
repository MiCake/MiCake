using MiCake.Core.Modularity;

namespace MiCake.Core.Tests.Modularity.Fakes
{
    [RelyOn(typeof(FeatureModuleBDepencyModuleA))]
    public class FeatureModuleCDencyModuleB : MiCakeModule, IFeatureModule
    {
        public FeatureModuleLoadOrder Order => FeatureModuleLoadOrder.AfterCommonModule;
    }
}
