using MiCake.AspNetCore;
using MiCake.Audit.Modules;
using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake
{
    public static class MiCakeAspNetCoreStart_ServiceCollectionExtension
    {
        /// <summary>
        /// Using MiCake's default configuration for aspnetcore
        /// </summary>
        /// <typeparam name="TEntryModule">Entry point module</typeparam>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="miCakeConfig">The config for MiCake application</param>
        /// <param name="miCakeEFConfig">The config for MiCake EFCore extension</param>
        /// <param name="miCakeAspNetConfig">The config for MiCake AspNetCore extension</param>
        public static IMiCakeBuilder AddMiCakeWithDefault<TEntryModule, TDbContext>(
                this IServiceCollection services,
                Action<MiCakeApplicationOptions> miCakeConfig = null,
                Action<MiCakeEFCoreOptions> miCakeEFConfig = null,
                Action<MiCakeAspNetOptions> miCakeAspNetConfig = null)
            where TDbContext : DbContext
            where TEntryModule : MiCakeModule
        {
            return AddMiCakeWithDefault(services, typeof(TEntryModule), typeof(TDbContext), miCakeConfig, miCakeEFConfig, miCakeAspNetConfig);
        }

        /// <summary>
        /// Using MiCake's default configuration for aspnetcore
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="entryModule">Entry point module</param>
        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(
            this IServiceCollection services,
            Type entryModule)
            where TDbContext : DbContext
        {
            return AddMiCakeWithDefault(services, entryModule, typeof(TDbContext), null);
        }

        /// <summary>
        /// Using MiCake's default configuration for aspnetcore
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="entryModule">Entry point module</param>
        /// <param name="miCakeConfig">The config for MiCake application</param>
        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions> miCakeConfig)
            where TDbContext : DbContext
        {
            return AddMiCakeWithDefault(services, entryModule, typeof(TDbContext), miCakeConfig, null);
        }

        /// <summary>
        /// Using MiCake's default configuration for aspnetcore
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="entryModule">Entry point module</param>
        /// <param name="miCakeConfig">The config for MiCake application</param>
        /// <param name="miCakeEFConfig">The config for MiCake EFCore extension</param>
        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions> miCakeConfig,
            Action<MiCakeEFCoreOptions> miCakeEFConfig)
            where TDbContext : DbContext
        {
            return AddMiCakeWithDefault(services, entryModule, typeof(TDbContext), miCakeConfig, miCakeEFConfig, null);
        }

        /// <summary>
        /// Using MiCake's default configuration for aspnetcore
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="entryModule">Entry point module</param>
        /// <param name="miCakeConfig">The config for MiCake application</param>
        /// <param name="miCakeEFConfig">The config for MiCake EFCore extension</param>
        /// <param name="miCakeAspNetConfig">The config for MiCake AspNetCore extension</param>
        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions> miCakeConfig,
            Action<MiCakeEFCoreOptions> miCakeEFConfig,
            Action<MiCakeAspNetOptions> miCakeAspNetConfig)
            where TDbContext : DbContext
        {
            return AddMiCakeWithDefault(services, entryModule, typeof(TDbContext), miCakeConfig, miCakeEFConfig, miCakeAspNetConfig);
        }

        /// <summary>
        /// Using MiCake's default configuration for aspnetcore
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="EntryModule">Entry point module</param>
        /// <param name="miCakeDbContextType"><see cref="MiCakeDbContext"/></param>
        /// <param name="miCakeConfig">The config for MiCake application</param>
        /// <param name="miCakeEFConfig">The config for MiCake EFCore extension</param>
        /// <param name="miCakeAspNetConfig">The config for MiCake AspNetCore extension</param>
        /// <returns></returns>
        public static IMiCakeBuilder AddMiCakeWithDefault(
            this IServiceCollection services,
            Type EntryModule,
            Type miCakeDbContextType,
            Action<MiCakeApplicationOptions> miCakeConfig = null,
            Action<MiCakeEFCoreOptions> miCakeEFConfig = null,
            Action<MiCakeAspNetOptions> miCakeAspNetConfig = null)
        {
            return services.AddMiCake(EntryModule, miCakeConfig)
                           .UseAudit()
                           .UseEFCore(miCakeDbContextType, miCakeEFConfig)
                           .UseAspNetCore(miCakeAspNetConfig);
        }
    }
}
