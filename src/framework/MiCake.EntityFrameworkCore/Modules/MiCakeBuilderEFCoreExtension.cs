using MiCake.Core.DependencyInjection;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    public static class MiCakeBuilderEFCoreExtension
    {
        /// <summary>
        /// Add MiCake EFCore services with default conventions.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore<TDbContext>(this IMiCakeBuilder builder)
            where TDbContext : DbContext
        {
            return UseEFCore<TDbContext>(builder, null);
        }

        /// <summary>
        /// Enable Entity Framework Core integration without any conventions
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type to configure</typeparam>
        /// <param name="builder">The MiCake builder instance</param>
        /// <param name="optionsBuilder">Optional configuration for MiCake EF Core options</param>
        /// <returns>The MiCake builder for method chaining</returns>
        public static IMiCakeBuilder UseEFCore<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreOptions>? optionsBuilder = null)
            where TDbContext : DbContext
        {
            return UseEFCore(builder, typeof(TDbContext), optionsBuilder);
        }

        /// <summary>
        /// Add MiCake EFCore services.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeDbContextType"><see cref="MiCakeDbContext"/></param>
        /// <param name="optionsBuilder">The config for MiCake EFCore extension</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore(
            this IMiCakeBuilder builder,
            Type miCakeDbContextType,
            Action<MiCakeEFCoreOptions>? optionsBuilder = null)
        {
            MiCakeEFCoreOptions options = new(miCakeDbContextType);
            optionsBuilder?.Invoke(options);

            // Register services directly on the builder's service collection
            builder.Services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(options);
            builder.GetApplicationOptions().BuildTimeData.Deposit(MiCakeEFCoreModuleInternalKeys.DBContextType, miCakeDbContextType);

            return builder;
        }
    }
}
