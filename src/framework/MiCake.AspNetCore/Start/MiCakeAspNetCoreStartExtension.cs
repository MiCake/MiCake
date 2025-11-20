using MiCake.AspNetCore;
using MiCake.Audit;
using MiCake.Core.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// Configuration options for MiCake default setup in ASP.NET Core applications.
    /// </summary>
    public class MiCakeSetupOptions
    {
        /// <summary>
        /// Configuration action for <see cref="MiCakeApplicationOptions"/>.
        /// </summary>
        public Action<MiCakeApplicationOptions> AppConfig { get; set; }

        /// <summary>
        /// Configuration action for <see cref="MiCakeAuditOptions"/>.
        /// </summary>
        public Action<MiCakeAuditOptions> AuditConfig { get; set; }

        /// <summary>
        /// Configuration action for <see cref="MiCakeAspNetOptions"/>.
        /// </summary>
        public Action<MiCakeAspNetOptions> AspNetConfig { get; set; }
    }

    public static class MiCakeAspNetCoreStartExtension
    {
        /// <summary>
        /// Adds MiCake with default configuration for ASP.NET Core applications.
        /// This method sets up MiCake with essential modules including audit, EF Core integration, and ASP.NET Core features.
        /// </summary>
        /// <typeparam name="TEntryModule">The entry point module for your MiCake application, must inherit from <see cref="MiCakeModule"/>.</typeparam>
        /// <typeparam name="TDbContext">The DbContext type for EF Core integration, must inherit from <see cref="DbContext"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOptions">Optional configuration action for all MiCake options.</param>
        /// <returns>The <see cref="IMiCakeBuilder"/> for further configuration.</returns>
        /// <example>
        /// Basic usage with default configuration:
        /// <code>
        /// services.AddMiCakeWithDefault&lt;MyEntryModule, MyDbContext&gt;();
        /// </code>
        /// 
        /// With custom configuration:
        /// <code>
        /// services.AddMiCakeWithDefault&lt;MyEntryModule, MyDbContext&gt;(options => {
        ///     options.AppConfig = config => {
        ///         config.DomainLayerAssemblies = new[] { typeof(MyEntryModule).Assembly };
        ///     };
        ///     options.AspNetConfig = aspNetConfig => {
        ///         aspNetConfig.UseDataWrapper = true;
        ///         aspNetConfig.DataWrapperOptions.SuccessCode = 200;
        ///     };
        /// });
        /// </code>
        /// </example>
        public static IMiCakeBuilder AddMiCakeWithDefault<TEntryModule, TDbContext>(
                this IServiceCollection services,
                Action<MiCakeSetupOptions>? configureOptions = null)
            where TDbContext : DbContext
            where TEntryModule : MiCakeModule
        {
            return AddMiCakeWithDefault(services, typeof(TEntryModule), typeof(TDbContext), configureOptions);
        }

        /// <summary>
        /// Adds MiCake with default configuration for ASP.NET Core applications using reflection-based module and DbContext types.
        /// This method sets up MiCake with essential modules including audit, EF Core integration, and ASP.NET Core features.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="entryModule">The entry point module type, must inherit from <see cref="MiCakeModule"/>.</param>
        /// <param name="miCakeDbContextType">The DbContext type for EF Core integration, must inherit from <see cref="DbContext"/>.</param>
        /// <param name="configureOptions">Optional configuration action for all MiCake options.</param>
        /// <returns>The <see cref="IMiCakeBuilder"/> for further configuration.</returns>
        /// <example>
        /// <code>
        /// services.AddMiCakeWithDefault(
        ///     typeof(MyEntryModule),
        ///     typeof(MyDbContext),
        ///     options => {
        ///         options.Config = config => {
        ///             config.DomainLayerAssemblies = new[] { typeof(MyEntryModule).Assembly };
        ///         };
        ///     }
        /// );
        /// </code>
        /// </example>
        public static IMiCakeBuilder AddMiCakeWithDefault(
            this IServiceCollection services,
            Type entryModule,
            Type miCakeDbContextType,
            Action<MiCakeSetupOptions>? configureOptions = null)
        {
            var options = new MiCakeSetupOptions();
            configureOptions?.Invoke(options);

            return services.AddMiCake(entryModule, options.AppConfig)
                           .UseAudit(options.AuditConfig)
                           .UseEFCore(miCakeDbContextType, null)
                           .UseAspNetCore(options.AspNetConfig);
        }
    }
}
