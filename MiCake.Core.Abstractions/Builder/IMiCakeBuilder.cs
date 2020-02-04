using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Abstractions.Builder
{
    public interface IMiCakeBuilder
    {
        public IMiCakeModuleManager ModuleManager { get; }

        public IServiceCollection Services { get; }
    }
}
