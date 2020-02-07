using System;
using System.Collections.Generic;
using System.Text;
using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Util;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core
{
    public class DefaultMiCakeApplicationProvider : MiCakeApplication, IMiCakeApplicationProvider
    {
        public DefaultMiCakeApplicationProvider(
            Type startUp,
            IServiceCollection services,
            MiCakeApplicationOptions options,
            Action<IMiCakeBuilder> builderConfigAction) : base(startUp, services, options, builderConfigAction)
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

            Init();
        }
    }
}
