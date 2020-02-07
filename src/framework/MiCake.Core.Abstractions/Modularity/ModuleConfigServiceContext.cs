using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Abstractions.Modularity
{
    public class ModuleConfigServiceContext
    {
        public IServiceCollection Services { get; private set; }
        public IMiCakeModuleCollection MiCakeModules { get; private set; }

        public ModuleConfigServiceContext(IServiceCollection services, IMiCakeModuleCollection miCakeModules)
        {
            Services = services;
            MiCakeModules = miCakeModules;
        }
    }
}
