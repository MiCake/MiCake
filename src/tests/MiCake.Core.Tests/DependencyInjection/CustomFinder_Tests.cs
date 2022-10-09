using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.Tests.DependencyInjection
{
    public class CustomFinder_Tests
    {
        public IServiceCollection Services { get; }

        public CustomFinder_Tests()
        {
            Services = BuildServiceCollection();
        }

        [Fact]
        public void UseCustomFinder_OnlyResigerIndicateServices()
        {
            FindAutoServiceTypesDelegate onlyFindSinglethonClass = (s, t) =>
            {
                return s.Name == "SinglethonClass" ? new List<Type>() { typeof(ISingletonService) } : new List<Type>();
            };

            DefaultServiceRegistrar defaultServiceRegistrar = new(Services);
            defaultServiceRegistrar.SetServiceTypesFinder(onlyFindSinglethonClass);

            defaultServiceRegistrar.Register(BuildCurrentModule(typeof(CurrentMiCakeModule)));

            Assert.NotEmpty(Services);

            var singlethonClass = Services.Where(service => service.ServiceType == typeof(ISingletonService)).FirstOrDefault();
            Assert.NotNull(singlethonClass);

            // this will be null,because we only find singlethon class.
            var scopedClass = Services.Where(service => service.ServiceType == typeof(IScopedService)).FirstOrDefault();
            Assert.Null(scopedClass);
        }

        public IServiceCollection BuildServiceCollection()
        {
            IServiceCollection Services = new ServiceCollection();

            return Services;
        }

        public IMiCakeModuleCollection BuildCurrentModule(Type entryModule)
        {
            IMiCakeModuleCollection miCakeModules = new MiCakeModuleCollection();
            miCakeModules.Add(new MiCakeModuleDescriptor(entryModule, (MiCakeModule)Activator.CreateInstance(entryModule)));

            return miCakeModules;
        }
    }
}
