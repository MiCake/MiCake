using MiCake.Core;
using MiCake.EntityFrameworkCore.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
            builder.UseEFCore<TDbContext>(null);
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
            Action<MiCakeEFCoreOptions>? optionsBuilder)
            where TDbContext : DbContext
        {
            return builder.UseEFCore(typeof(TDbContext), optionsBuilder);
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
            Action<MiCakeEFCoreOptions>? optionsBulder)
        {
            MiCakeEFCoreOptions options = new();
            optionsBulder?.Invoke(options);

            builder.ConfigureApplication((app, services) =>
            {
                app.SlotModule(typeof(MiCakeEFCoreModule));

                services.Configure<EFCoreDbContextTypeAccessor>(s => s.DbContextType = miCakeDbContextType);
            });

            return builder;
        }
    }
}
