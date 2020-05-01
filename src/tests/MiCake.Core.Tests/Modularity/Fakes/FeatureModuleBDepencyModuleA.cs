using MiCake.Core.Modularity;

namespace MiCake.Core.Tests.Modularity.Fakes
{
    [RelyOn(typeof(FeatureModuleA))]
    public class FeatureModuleBDepencyModuleA : MiCakeModule, IFeatureModule
    {
        public FeatureModuleLoadOrder Order => FeatureModuleLoadOrder.BeforeCommonModule;
    }
}
