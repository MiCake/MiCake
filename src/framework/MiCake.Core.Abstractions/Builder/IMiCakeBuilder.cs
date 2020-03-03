using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Builder
{
    public interface IMiCakeBuilder
    {
        public IMiCakeModuleManager ModuleManager { get; }

        public IServiceCollection Services { get; }
    }
}
