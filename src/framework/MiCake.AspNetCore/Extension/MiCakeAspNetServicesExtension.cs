using MiCake.Core;
using MiCake.Core.Builder;
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
        public static IMiCakeApplication AddMiCake()
        {
            var entryAsm = Assembly.GetEntryAssembly();
            return null;
        }

        public static IMiCakeApplication AddMiCake(this IServiceCollection services, Type startModule)
        {
            return AddMiCake(services, startModule, null);
        }

        public static IMiCakeApplication AddMiCake(
            this IServiceCollection services,
            Type startModule,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            return AddMiCake(services, startModule, new MiCakeApplicationOptions(), builderConfigAction);
        }

        public static IMiCakeApplication AddMiCake(
            this IServiceCollection services,
            Type startModule,
            MiCakeApplicationOptions options,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            return MiCakeApplictionFactory.Create(services, startModule, options, builderConfigAction);
        }

        public static IMiCakeApplication AddMiCake<TStartupModule>(this IServiceCollection services)
        {
            return AddMiCake(services, typeof(TStartupModule));
        }

        public static IMiCakeApplication AddMiCake<TStartupModule>(
            this IServiceCollection services,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            return AddMiCake(services, typeof(TStartupModule), builderConfigAction);
        }

        public static IMiCakeApplication AddMiCake<TStartupModule>(
            this IServiceCollection services,
            MiCakeApplicationOptions options,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            return AddMiCake(services, typeof(TStartupModule), options, builderConfigAction);
        }
    }
}
