using MiCake.Core.Builder;
using MiCake.Core.Modularity;

namespace MiCake.Autofac
{
    public static class MiCakeApplicationAutofacExtension
    {
        public static IMiCakeBuilder UseAutofac(this IMiCakeBuilder builder)
        {
            builder.ModuleManager.AddFeatureModule(new MiCakeAutofacModule() { AutoRegisted = true, Order = FeatureModuleLoadOrder.BeforeCommonModule });

            return builder;
        }
    }
}
