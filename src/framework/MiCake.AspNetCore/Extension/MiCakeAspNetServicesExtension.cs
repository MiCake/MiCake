using MiCake.Core;
using MiCake.Core.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.AspNetCore.Extension
{
    public static class MiCakeAspNetServicesExtension
    {
        public static IMiCakeApplication AddMiCake<TStartupModule>(this IServiceCollection services)
        {
            return MiCakeApplictionFactory.Create<TStartupModule>(services);
        }

        public static IMiCakeApplication AddMiCake<TStartupModule>(
            this IServiceCollection services,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            MiCakeApplicationOptions defaultOptions = new MiCakeApplicationOptions();
            return MiCakeApplictionFactory.Create<TStartupModule>(services, defaultOptions, builderConfigAction);
        }

        public static IMiCakeApplication AddMiCake<TStartupModule>(
            this IServiceCollection services,
            MiCakeApplicationOptions options,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            return MiCakeApplictionFactory.Create<TStartupModule>(services, options, builderConfigAction);
        }
    }
}
