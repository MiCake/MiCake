using MiCake.Core;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Cord.Tests
{
    public abstract class MiCakeDDDTestsBase
    {
        public IMiCakeModuleContext MiCakeModuleContext { get; set; }

        public IServiceCollection Services { get; set; }

        public MiCakeDDDTestsBase()
        {
            //ADD Current MiCakeModule
            var testModuleType = typeof(MiCakeDDDTestModule);
            var currentModule = new MiCakeModuleDescriptor(testModuleType, (MiCakeModule)Activator.CreateInstance(testModuleType));

            var moduleContext = new MiCakeModuleContext();
            moduleContext.MiCakeModules.Add(currentModule);
            moduleContext.MiCakeModules.Add(currentModule);

            MiCakeModuleContext = moduleContext;

            //ServiceCollection
            Services = new ServiceCollection();

            Services.AddSingleton<IMiCakeModuleContext>(moduleContext);
            //AppOptions 
            Services.AddOptions<MiCakeApplicationOptions>();
        }
    }
}
