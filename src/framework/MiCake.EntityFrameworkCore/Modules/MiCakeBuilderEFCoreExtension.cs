using MiCake.Core;
using MiCake.Core.DependencyInjection;
using MiCake.EntityFrameworkCore.Modules;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore
{
    public static class MiCakeBuilderEFCoreExtension
    {
        /// <summary>
        /// Add MiCake EFCore services.
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore<TDbContext>(this IMiCakeBuilder builder)
            where TDbContext : DbContext
        {
            UseEFCore<TDbContext>(builder, null);
            return builder;
        }

        /// <summary>
        /// Add MiCake EFCore services.
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="optionsBuilder">The config for MiCake EFCore extension</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreOptions> optionsBuilder)
            where TDbContext : DbContext
        {
            return UseEFCore(builder, typeof(TDbContext), optionsBuilder);
        }

        /// <summary>
        /// Add MiCake EFCore services.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeDbContextType"><see cref="MiCakeDbContext"/></param>
        /// <param name="optionsBulder">The config for MiCake EFCore extension</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
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

                //add efcore uow services.
                services.AddUowCoreServices(miCakeDbContextType);
            });

            return builder;
        }
    }
}
