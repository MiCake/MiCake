using MiCake.AspNetCore;
using MiCake.AspNetCore.Start;
using MiCake.Audit;
using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace MiCake
{
    public static class MiCakeAspNetCoreStart_ServiceCollectionExtension
    {
        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(this IServiceCollection services)
            where TDbContext : MiCakeDbContext
        {
            var dynamicEntryModule = DynamicMiCakeEntryModuleHelper.CreateDynamicEntryModule();
            return AddMiCakeWithDefault(services, dynamicEntryModule, typeof(TDbContext), null);
        }

        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext, TEntryModule>(this IServiceCollection services)
            where TDbContext : MiCakeDbContext
            where TEntryModule : MiCakeModule
        {
            return AddMiCakeWithDefault(services, typeof(TEntryModule), typeof(TDbContext), null);
        }

        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(
            this IServiceCollection services,
            Type entryModule)
            where TDbContext : MiCakeDbContext
        {
            return AddMiCakeWithDefault(services, entryModule, typeof(TDbContext), null);
        }

        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions> miCakeConfig)
            where TDbContext : MiCakeDbContext
        {
            return AddMiCakeWithDefault(services, entryModule, typeof(TDbContext), miCakeConfig, null);
        }

        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions> miCakeConfig,
            Action<MiCakeEFCoreOptions> miCakeEFConfig)
            where TDbContext : MiCakeDbContext
        {
            return AddMiCakeWithDefault(services, entryModule, typeof(TDbContext), miCakeConfig, miCakeEFConfig, null);
        }

        public static IMiCakeBuilder AddMiCakeWithDefault<TDbContext>(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions> miCakeConfig,
            Action<MiCakeEFCoreOptions> miCakeEFConfig,
            Action<MiCakeAspNetOptions> miCakeAspNetConfig)
            where TDbContext : MiCakeDbContext
        {
            return AddMiCakeWithDefault(services, entryModule, typeof(TDbContext), miCakeConfig, miCakeEFConfig, miCakeAspNetConfig);
        }

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
