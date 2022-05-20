using MiCake.AspNetCore;
using MiCake.Audit;
using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake
{
    public static class MiCakeAspNetCoreStart_ServiceCollectionExtension
    {
        /// <summary>
        /// Add MiCake services with whole characters, including auto audit,auto uow save,auto wrap data,etc.
        /// <para>
        ///     you must specify <paramref name="auditTimeGenerateSql"/>,you can get some preset value from <see cref="PresetAuditConstants"/>.
        /// </para>
        /// </summary>
        /// <typeparam name="TEntryModule">Entry point module</typeparam>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="auditTimeGenerateSql"></param>
        public static IMiCakeBuilder AddMiCakeServices<TEntryModule, TDbContext>(
                this IServiceCollection services,
                string auditTimeGenerateSql)
            where TDbContext : DbContext
            where TEntryModule : MiCakeModule
        {
            return AddMiCakeServices(services, typeof(TEntryModule), typeof(TDbContext), s => { s.AuditOptions.TimeGenerateSql = auditTimeGenerateSql; });
        }

        /// <summary>
        /// Add MiCake services with whole characters, including auto audit,auto uow save,auto wrap data,etc.
        /// </summary>
        /// <typeparam name="TEntryModule">Entry point module</typeparam>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="options"></param>
        public static IMiCakeBuilder AddMiCakeServices<TEntryModule, TDbContext>(
                this IServiceCollection services,
                Action<MiCakeStartupOptions>? options = null)
            where TDbContext : DbContext
            where TEntryModule : MiCakeModule
        {
            return AddMiCakeServices(services, typeof(TEntryModule), typeof(TDbContext), options);
        }


        /// <summary>
        /// Add MiCake services with whole characters, including auto audit,auto uow save,auto wrap data,etc.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="EntryModule">Entry point module</param>
        /// <param name="miCakeDbContextType"><see cref="MiCakeDbContext"/></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IMiCakeBuilder AddMiCakeServices(
            this IServiceCollection services,
            Type EntryModule,
            Type miCakeDbContextType,
            Action<MiCakeStartupOptions>? options = null)
        {
            var startUpOptions = new MiCakeStartupOptions();
            options?.Invoke(startUpOptions);

            CheckOptions(startUpOptions);

            var appOptions = startUpOptions.ApplicationOptions;
            var efcoreOptions = startUpOptions.EFCoreOptions;
            var auditOptions = startUpOptions.AuditOptions;
            var aspnetCoreOptions = startUpOptions.AspNetOptions;

            return services.AddMiCake(EntryModule, s => s.Apply(appOptions))
                           .UseAudit(s => s.Apply(auditOptions))
                           .UseEFCore(miCakeDbContextType, s => s.Apply(efcoreOptions))
                           .UseAspNetCore(s => s.Apply(aspnetCoreOptions));
        }

        private static void CheckOptions(MiCakeStartupOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.AuditOptions.TimeGenerateSql))
            {
                throw new ArgumentException($"You are using MiCake audit module,please config {options.AuditOptions.TimeGenerateSql}. you can get more preset vaule in {nameof(PresetAuditConstants)} class.");
            }
        }
    }
}
