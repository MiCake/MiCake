using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Modularity
{
    public class ModuleConfigServiceContext(
        IServiceCollection services,
        IMiCakeModuleCollection miCakeModules,
        MiCakeApplicationOptions miCakeApplication)
    {
        public IServiceCollection Services { get; private set; } = services;
        public IMiCakeModuleCollection MiCakeModules { get; private set; } = miCakeModules;
        public MiCakeApplicationOptions MiCakeApplicationOptions { get; private set; } = miCakeApplication;
    }
}
