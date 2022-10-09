using MiCake.AspNetCore.Modules;
using MiCake.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.AspNetCore
{
    public static class MiCakeBuilderAspNetCoreExtension
    {
        /// <summary>
        /// Add MiCake AspnetCore services.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseAspNetCore(this IMiCakeBuilder builder)
        {
            UseAspNetCore(builder, null);
            return builder;
        }

        /// <summary>
        /// Add MiCake AspnetCore services.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="optionsBulder">The config for MiCake AspNetCore extension</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseAspNetCore(
            this IMiCakeBuilder builder,
            Action<MiCakeAspNetCoreOptions>? optionsBulder)
        {
            builder.ConfigureApplication((app, services) =>
            {
                app.SlotModule<MiCakeAspNetCoreModule>();

                services.Configure<MiCakeAspNetCoreOptions>(options =>
                {
                    optionsBulder?.Invoke(options);
                });
            });

            return builder;
        }
    }
}
