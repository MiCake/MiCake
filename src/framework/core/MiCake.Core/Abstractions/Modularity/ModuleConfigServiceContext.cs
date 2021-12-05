using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core.Modularity
{
    public class ModuleConfigServiceContext
    {
        public IServiceCollection Services { get; private set; }
        public IMiCakeModuleCollection MiCakeModules { get; private set; }
        public MiCakeApplicationOptions MiCakeApplicationOptions { get; private set; }

        public ModuleConfigServiceContext(
            IServiceCollection services,
            IMiCakeModuleCollection miCakeModules,
            MiCakeApplicationOptions miCakeApplication)
        {
            Services = services;
            MiCakeModules = miCakeModules;
            MiCakeApplicationOptions = miCakeApplication;
        }
    }
}
