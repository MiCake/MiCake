using MiCake.Core;
using Microsoft.Extensions.DependencyInjection;
using System;

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
            Action<MiCakeAspNetOptions>? optionsBulder)
        {
            // Configure services directly on the builder's service collection
            builder.Services.Configure<MiCakeAspNetOptions>(options =>
            {
                optionsBulder?.Invoke(options);
            });
            
            // MiCakeAspNetCoreModule should be added through module dependency ([RelyOn] attribute)
            // by user's entry module if they want to use ASP.NET Core features

            return builder;
        }
    }
}
