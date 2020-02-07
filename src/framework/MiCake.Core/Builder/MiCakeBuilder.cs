using MiCake.Core.Abstractions.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using MiCake.Core.Abstractions.Modularity;

namespace MiCake.Core.Builder
{
    public class MiCakeBuilder : IMiCakeBuilder
    {
        public IMiCakeModuleManager ModuleManager { get; }

        public IServiceCollection Services { get; }

        public MiCakeBuilder(IServiceCollection services, IMiCakeModuleManager manager)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            Services = services;
            ModuleManager = manager;
        }
    }
}
