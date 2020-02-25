using MiCake.Core.Modularity;

namespace MiCake.Core.Tests.Modularity.Fakes
{
    [DependOn(typeof(FeatureModuleA))]
    public class FeatureModuleBDepencyModuleA : MiCakeModule, IFeatureModule
    {
        public FeatureModuleLoadOrder Order => FeatureModuleLoadOrder.BeforeCommonModule;
    }
}
