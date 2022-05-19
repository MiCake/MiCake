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
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="entryModule">Entry point module</param>
        public static IMiCakeBuilder AddMiCakeServices<TDbContext>(
            this IServiceCollection services,
            Type entryModule)
            where TDbContext : DbContext
        {
            return AddMiCakeServices(services, entryModule, typeof(TDbContext), null);
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

            var appOptions = startUpOptions.ApplicationOptions;
            var efcoreOptions = startUpOptions.EFCoreOptions;
            var auditOptions = startUpOptions.AuditOptions;
            var aspnetCoreOptions = startUpOptions.AspNetOptions;

            return services.AddMiCake(EntryModule, s => s = appOptions)
                           .UseAudit(s => s = auditOptions)
                           .UseEFCore(miCakeDbContextType, s => s = efcoreOptions)
                           .UseAspNetCore(s => s = aspnetCoreOptions);
        }
    }
}
