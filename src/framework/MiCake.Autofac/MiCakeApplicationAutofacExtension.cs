using MiCake.Core.Builder;

namespace MiCake.Autofac
{
    public static class MiCakeApplicationAutofacExtension
    {
        public static IMiCakeBuilder UseAutofac(this IMiCakeBuilder builder)
        {
            builder.ModuleManager.AddFeatureModule(typeof(MiCakeAutofacModule));

            return builder;
        }
    }
}
