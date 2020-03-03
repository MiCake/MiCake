using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core.Builder
{
    public class MiCakeBuilder : IMiCakeBuilder
    {
        public IMiCakeModuleManager ModuleManager { get; }

        public IServiceCollection Services { get; }

        public MiCakeBuilder(
            IServiceCollection services,
            IMiCakeModuleManager manager)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            ModuleManager = manager ?? throw new ArgumentNullException(nameof(manager));
        }
    }
}
