using MiCake.Core;
using MiCake.Core.DependencyInjection;
using MiCake.EntityFrameworkCore.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore
{
    public static class MiCakeBuilderEFCoreExtesion
    {
        public static IMiCakeBuilder UseEFCore<TDbContext>(this IMiCakeBuilder builder)
        {
            UseEFCore<TDbContext>(builder, null);
            return builder;
        }

        public static IMiCakeBuilder UseEFCore<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreOptions> optionsBulder)
        {
            MiCakeEFCoreOptions options = new MiCakeEFCoreOptions(typeof(TDbContext));
            optionsBulder?.Invoke(options);

            builder.ConfigureApplication((app, services) =>
            {
                //register ef module to micake module collection
                app.ModuleManager.AddMiCakeModule(typeof(MiCakeEFCoreModule));

                services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(options);
            });

            return builder;
        }
    }
}
