using MiCake.AspNetCore.ExceptionHandling;
using MiCake.Core;
using MiCake.Core.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace MiCake
{
    public static class MiCakeAspNetServicesExtension
    {
        /// <summary>
        /// This method can only be used when the program only has one project.
        /// Please use a more accurate startup method <see cref="AddMiCake{TStartupModule}(IServiceCollection)"/>
        /// </summary>
        public static IMiCakeBuilder AddMiCake()
        {
            var entryAsm = Assembly.GetEntryAssembly();
            return null;
        }

        public static IMiCakeBuilder AddMiCake(
            this IServiceCollection services,
            Type entryModule)
        {
            return AddMiCake(services, entryModule, null);
        }

        public static IMiCakeBuilder AddMiCake(
            this IServiceCollection services,
            Type entryModule,
            Action<MiCakeApplicationOptions> configOptions,
            bool needNewScope = false)
        {
            MiCakeApplicationOptions options = new MiCakeApplicationOptions();

            configOptions?.Invoke(options);

            return new DefaultMiCakeBuilderProvider(services, entryModule, options, needNewScope).GetMiCakeBuilder();
        }

        public static IMiCakeBuilder AddMiCake<TStartupModule>(this IServiceCollection services)
        {
            return AddMiCake(services, typeof(TStartupModule));
        }

        public static IMiCakeBuilder AddMiCake<TStartupModule>(
            this IServiceCollection services,
            Action<MiCakeApplicationOptions> configOptions,
            bool needNewScope = false)
        {
            return AddMiCake(services, typeof(TStartupModule), configOptions, needNewScope);
        }

        public static void StartMiCake(this IApplicationBuilder applicationBuilder)
        {
            var micakeApp = applicationBuilder.ApplicationServices.GetService<IMiCakeApplication>() ??
                                    throw new NullReferenceException($"Cannot find the instance of {nameof(IMiCakeApplication)}," +
                                    $"Please Check your has already AddMiCake() in ConfigureServices method");

            if (micakeApp is INeedNecessaryParts<IServiceProvider> needServiceProvider)
            {
                needServiceProvider.SetNecessaryParts(applicationBuilder.ApplicationServices);
            }
            //Add middlerware
            applicationBuilder.UseMiddleware<ExceptionHandlerMiddleware>();

            micakeApp.Start();
        }

        public static void ShutdownMiCake(this IApplicationBuilder applicationBuilder)
        {
            var micakeApp = applicationBuilder.ApplicationServices.GetService<IMiCakeApplication>() ??
                                    throw new NullReferenceException($"Cannot find the instance of {nameof(IMiCakeApplication)}," +
                                    $"Please Check your has already AddMiCake() in ConfigureServices method");

            micakeApp.ShutDown();
        }
    }
}
