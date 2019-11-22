using MiCake.Core;
using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.AspNetCore.Extension
{
    public static class MiCakeAspNetSericesExtension
    {
        public static IMiCakeApplication AddMiCake<TStartupModule>(this IServiceCollection services)
        {
            return MiCakeApplictionFactory.Create<TStartupModule>(services);
        }

        public static IMiCakeApplication AddMiCake<TStartupModule>(this IServiceCollection services, Action<IMiCakeBuilder> builderConfigAction)
        {
            return MiCakeApplictionFactory.Create<TStartupModule>(services, builderConfigAction);
        }
    }
}
