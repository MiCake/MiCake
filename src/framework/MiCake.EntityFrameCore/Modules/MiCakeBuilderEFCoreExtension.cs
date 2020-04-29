using MiCake.Core;
using MiCake.Core.DependencyInjection;
using MiCake.EntityFrameworkCore.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore
{
    public static class MiCakeBuilderEFCoreExtension
    {
        public static IMiCakeBuilder UseEFCore<TDbContext>(this IMiCakeBuilder builder)
            where TDbContext : MiCakeDbContext
        {
            UseEFCore<TDbContext>(builder, null);
            return builder;
        }

        public static IMiCakeBuilder UseEFCore<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreOptions> optionsBuilder)
            where TDbContext : MiCakeDbContext
        {
            return UseEFCore(builder, typeof(TDbContext), optionsBuilder);
        }

        public static IMiCakeBuilder UseEFCore(
            this IMiCakeBuilder builder,
            Type miCakeDbContextType,
            Action<MiCakeEFCoreOptions> optionsBulder)
        {
            MiCakeEFCoreOptions options = new MiCakeEFCoreOptions(miCakeDbContextType);
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
