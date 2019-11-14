using MiCake.Core;
using MiCake.Core.Abstractions;
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
    }
}
