using MiCake.Core;
using MiCake.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.AspNetCore.Extension
{
    public static class MiCakeAspNetSericesExtension
    {
        public static IServiceCollection AddMiCake<TStartupModule>(this IServiceCollection services, Action<IMiCakeApplicationOption> optionAction = null)
        {
            MiCakeApplictionFactory.Create<TStartupModule>(services, optionAction);
            return services;
        }
    }
}
