using MiCake.Core;

namespace MiCake.Autofac
{
    public static class MiCakeApplicationAutofacExtension
    {
        public static IMiCakeBuilder AddAutofac(this IMiCakeBuilder builder)
        {
            builder.ConfigureApplication(app =>
            {
                app.ModuleManager.AddFeatureModule(typeof(MiCakeAutofacModule));
            });

            return builder;
        }
    }
}
