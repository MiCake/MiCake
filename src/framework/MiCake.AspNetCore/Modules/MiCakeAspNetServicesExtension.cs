using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake
{
    /// <summary>
    /// Extension methods for integrating MiCake with ASP.NET Core
    /// </summary>
    public static class MiCakeAspNetServicesExtension
    {
        /// <summary>
        /// Add MiCake Core Service
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="entryModule">Entry point module</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder AddMiCake(
            this IServiceCollection services,
            Type entryModule)
        {
            return AddMiCake(services, entryModule, null);
        }

        /// <summary>
        /// Add MiCake Core Service
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="entryModule">Entry point module</param>
        /// <param name="configOptions">The config for MiCake application</param>
        /// <param name="needNewScope">New use new service scope to resolve micake core service</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder AddMiCake(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions> configOptions,
            bool needNewScope = false)
        {
            MiCakeApplicationOptions options = new();

            configOptions?.Invoke(options);

            var builder = new DefaultMiCakeBuilderProvider(services, entryModule, options).GetMiCakeBuilder();
            
            // Essential module is automatically added through module dependency discovery
            // MiCakeEssentialModule should be marked with [RelyOn] by user modules
            
            return builder;
        }

        /// <summary>
        /// Add MiCake Core Service
        /// </summary>
        /// <typeparam name="TEntryModule">Entry point module</typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder AddMiCake<TEntryModule>(this IServiceCollection services)
            where TEntryModule : MiCakeModule
        {
            return AddMiCake(services, typeof(TEntryModule));
        }

        /// <summary>
        /// Add MiCake Core Service
        /// </summary>
        /// <typeparam name="TEntryModule">Entry point module</typeparam>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="configOptions">The config for MiCake application</param>
        /// <param name="needNewScope">New use new service scope to resolve micake core service</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder AddMiCake<TEntryModule>(
            this IServiceCollection services,
            Action<MiCakeApplicationOptions> configOptions,
            bool needNewScope = false)
             where TEntryModule : MiCakeModule
        {
            return AddMiCake(services, typeof(TEntryModule), configOptions, needNewScope);
        }

        /// <summary>
        /// Start MiCake application.
        /// This method initializes and starts all registered MiCake modules.
        /// </summary>
        /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/></param>
        public static void StartMiCake(this IApplicationBuilder applicationBuilder)
        {
            var micakeApp = applicationBuilder.ApplicationServices.GetService<IMiCakeApplication>() ??
                                    throw new InvalidOperationException(
                                        $"Cannot find the instance of {nameof(IMiCakeApplication)}. " +
                                        $"Please ensure you have called AddMiCake() in ConfigureServices method.");

            // Start the application
            micakeApp.Start();
        }

        /// <summary>
        /// Shut down MiCake application.
        /// This method gracefully shuts down all registered MiCake modules.
        /// </summary>
        /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/></param>
        public static void ShutdownMiCake(this IApplicationBuilder applicationBuilder)
        {
            var micakeApp = applicationBuilder.ApplicationServices.GetService<IMiCakeApplication>() ??
                                    throw new InvalidOperationException(
                                        $"Cannot find the instance of {nameof(IMiCakeApplication)}. " +
                                        $"Please ensure you have called AddMiCake() in ConfigureServices method.");

            micakeApp.ShutDown();
        }
    }
}
