using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.DDD.Connector.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.DDD.Tests
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
            moduleContext.AllModules.Add(currentModule);
            moduleContext.MiCakeModules.Add(currentModule);

            MiCakeModuleContext = moduleContext;

            //ServiceCollection
            Services = new ServiceCollection();

            Services.AddSingleton<IMiCakeModuleContext>(moduleContext);
            //AppOptions 
            Services.AddOptions<MiCakeApplicationOptions>();
        }

        protected void BuildServiceCollection()
        {
            Services.AddTransient<IDomainObjectModelProvider, DefaultDomainObjectModelProvider>();
            Services.AddSingleton<DomainObjectFactory>();
            Services.AddSingleton<IDomainMetadataProvider, DomainMetadataProvider>();
            Services.AddSingleton<DomainMetadata>(factory =>
            {
                var provider = factory.GetService<IDomainMetadataProvider>();
                return provider.GetDomainMetadata();
            });
        }
    }
}
