using MiCake.Core.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.AspNetCore.Extension
{
    public static class MiCakeBuilderAspNetCoreExtesion
    {
        public static IMiCakeBuilder UseAspNetCore(this IMiCakeBuilder builder)
        {
            UseAspNetCore(builder, null);
            return builder;
        }

        public static IMiCakeBuilder UseAspNetCore(
            this IMiCakeBuilder builder,
            Action<MiCakeAspNetOptions> optionsBulder)
        {
            MiCakeAspNetOptions defaultOptions = new MiCakeAspNetOptions();
            optionsBulder?.Invoke(defaultOptions);

            builder.Services.AddOptions<MiCakeAspNetOptions>();

            return builder;
        }
    }
}
