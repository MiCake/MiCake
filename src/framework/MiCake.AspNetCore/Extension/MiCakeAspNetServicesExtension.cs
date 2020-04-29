using MiCake.AspNetCore;
using MiCake.AspNetCore.ExceptionHandling;
using MiCake.Core;
using MiCake.Core.Data;
using MiCake.Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace MiCake
{
    public static class MiCakeAspNetServicesExtension
    {
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
            where TStartupModule : MiCakeModule
        {
            return AddMiCake(services, typeof(TStartupModule));
        }

        public static IMiCakeBuilder AddMiCake<TStartupModule>(
            this IServiceCollection services,
            Action<MiCakeApplicationOptions> configOptions,
            bool needNewScope = false)
             where TStartupModule : MiCakeModule
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

            var micakeAspnetOption = applicationBuilder.ApplicationServices.GetService<IOptions<MiCakeAspNetOptions>>().Value;

            //Add middlerware
            if (micakeAspnetOption.UseDataWrapper)
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
