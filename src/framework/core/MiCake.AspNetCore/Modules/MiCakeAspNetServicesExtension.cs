using MiCake.Core;
using MiCake.Core.Data;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake
{
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
        /// <param name="needNewServiceScope">New use new service scope to resolve micake core service</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder AddMiCake(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions>? configOptions,
            bool needNewServiceScope = false)
        {
            MiCakeApplicationOptions options = new();

            configOptions?.Invoke(options);

            var builder = new DefaultMiCakeBuilderProvider(services, entryModule, options, needNewServiceScope).GetMiCakeBuilder();

            // add Core module.
            builder.ConfigureApplication((s) =>
            {
                s.SlotModule<MiCakeDIModule>();
            });

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
        /// <param name="needNewServiceScope">New use new service scope to resolve micake core service</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder AddMiCake<TEntryModule>(
            this IServiceCollection services,
            Action<MiCakeApplicationOptions> configOptions,
            bool needNewServiceScope = false)
             where TEntryModule : MiCakeModule
        {
            return AddMiCake(services, typeof(TEntryModule), configOptions, needNewServiceScope);
        }

        /// <summary>
        /// Start MiCake application.
        /// </summary>
        /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/></param>
        public static void StartMiCake(this IApplicationBuilder applicationBuilder)
        {
            var micakeApp = applicationBuilder.ApplicationServices.GetService<IMiCakeApplication>() ??
                                    throw new NullReferenceException($"Cannot find the instance of {nameof(IMiCakeApplication)}," +
                                    $"Please Check your has already AddMiCake() in ConfigureServices method");

            if (micakeApp is IHasSupplement<IServiceProvider> needServiceProvider)
            {
                needServiceProvider.SetData(applicationBuilder.ApplicationServices);
            }

            micakeApp.Start();
        }

        /// <summary>
        /// Shut down MiCake application.
        /// </summary>
        /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/></param>
        public static void ShutdownMiCake(this IApplicationBuilder applicationBuilder)
        {
            var micakeApp = applicationBuilder.ApplicationServices.GetService<IMiCakeApplication>() ??
                                    throw new NullReferenceException($"Cannot find the instance of {nameof(IMiCakeApplication)}," +
                                    $"Please Check your has already AddMiCake() in ConfigureServices method");

            micakeApp.ShutDown();
        }
    }
}
