using MiCake.Core.Modularity;

namespace MiCake.Core.Tests.Modularity.Fakes
{
    public class FeatureModuleA : MiCakeModule, IFeatureModule
    {
        public FeatureModuleA()
        {
        }

        public FeatureModuleLoadOrder Order => FeatureModuleLoadOrder.AfterCommonModule;
    }
}
