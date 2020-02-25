using MiCake.Core.Modularity;

namespace MiCake.Core.Tests.Modularity.Fakes
{
    [DependOn(typeof(FeatureModuleBDepencyModuleA))]
    public class FeatureModuleCDencyModuleB : MiCakeModule, IFeatureModule
    {
        public FeatureModuleLoadOrder Order => FeatureModuleLoadOrder.AfterCommonModule;
    }
}
