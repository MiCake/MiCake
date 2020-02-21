using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;

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
