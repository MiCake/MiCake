using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Abstractions.Modularity
{
    public struct ModuleConfigServiceContext
    {
        public IServiceCollection Services { get;  }

        public ModuleConfigServiceContext(IServiceCollection services)
        {
            Services = services;
        }
    }
}
