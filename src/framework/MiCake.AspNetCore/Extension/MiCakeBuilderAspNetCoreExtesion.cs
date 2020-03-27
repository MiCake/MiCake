using MiCake.AspNetCore.Modules;
using MiCake.Core;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.AspNetCore
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

            builder.ConfigureApplication((app, services) =>
            {
                app.ModuleManager.AddMiCakeModule(typeof(MiCakeAspNetCoreModule));
                services.AddOptions<MiCakeAspNetOptions>();
            });

            return builder;
        }
    }
}
