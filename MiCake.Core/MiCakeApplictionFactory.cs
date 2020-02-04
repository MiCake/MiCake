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
            MiCakeApplicationOptions defalutOptions = new MiCakeApplicationOptions();
            return new DefaultMiCakeApplicationProvider(typeof(TStartupModule), services, defalutOptions, null);
        }

        public static IMiCakeApplication Create<TStartupModule>(
            IServiceCollection services, 
            MiCakeApplicationOptions options, 
            Action<IMiCakeBuilder> builderConfigAction)
        {
            return new DefaultMiCakeApplicationProvider(typeof(TStartupModule), services, options, builderConfigAction);
        }
    }
}
