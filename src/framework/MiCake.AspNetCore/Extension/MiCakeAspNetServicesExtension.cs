using MiCake.Core;
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
    }
}
