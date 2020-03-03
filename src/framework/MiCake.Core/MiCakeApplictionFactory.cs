using JetBrains.Annotations;
using MiCake.Core.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    public class MiCakeApplictionFactory
    {
        public static IMiCakeApplication Create(
            [NotNull]IServiceCollection services,
            [NotNull]Type startModule,
            [NotNull]MiCakeApplicationOptions options,
            Action<IMiCakeBuilder> builderConfigAction)
        {
            return new DefaultMiCakeApplicationProvider(startModule, services, options, builderConfigAction);
        }
    }
}
