using MiCake.Core.Builder;
using MiCake.Core.Util;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    public class DefaultMiCakeApplicationProvider : MiCakeApplication, IMiCakeApplicationProvider
    {
        public DefaultMiCakeApplicationProvider(
            Type startUp,
            IServiceCollection services,
            MiCakeApplicationOptions options,
            Action<IMiCakeBuilder> builderConfigAction) : base(services, options, builderConfigAction)
        {
            services.AddSingleton<IMiCakeApplicationProvider>(this);
        }

        public IMiCakeApplication GetApplication()
        {
            return this;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            CheckValue.NotNull(serviceProvider, nameof(serviceProvider));

            SetServiceProvider(serviceProvider);

            Start();
        }
    }
}
