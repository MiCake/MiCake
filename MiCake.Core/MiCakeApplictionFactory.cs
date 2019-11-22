using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    public class MiCakeApplictionFactory
    {
        public static IMiCakeApplication Create<TStartupModule>(IServiceCollection services)
        {
            return new DefaultMiCakeApplicationProvider(typeof(TStartupModule), services, null);
        }

        public static IMiCakeApplication Create<TStartupModule>(IServiceCollection services, Action<IMiCakeBuilder> builderConfigAction)
        {
            return new DefaultMiCakeApplicationProvider(typeof(TStartupModule), services, builderConfigAction);
        }
    }
}
