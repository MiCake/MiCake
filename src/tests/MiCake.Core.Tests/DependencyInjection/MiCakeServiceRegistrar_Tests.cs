using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.Core.Tests.DependencyInjection.Fakes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace MiCake.Core.Tests.DependencyInjection
{
    public class MiCakeServiceRegistrar_Tests
    {
        public IMiCakeModuleCollection MiCakeModules { get; set; }

        public IServiceCollection Services { get; set; }

        public MiCakeServiceRegistrar_Tests()
        {
            MiCakeModules = BuildCurrentModule();
            Services = BuildServiceCollection();

            DefaultServiceRegistrar defaultServiceRegistrar = new(Services);
            defaultServiceRegistrar.Register(MiCakeModules);
        }

        [Fact]
        public void InheritInterface_ShouldHasCurrentLifeTime()
        {
            var singlethonClass = Services.FirstOrDefault(service => service.ImplementationType == typeof(SinglethonClass));

            Assert.NotNull(singlethonClass);
            Assert.Equal(ServiceLifetime.Singleton, singlethonClass.Lifetime);

            var transientClass = Services.FirstOrDefault(service => service.ImplementationType == typeof(TransientClass));

            Assert.NotNull(transientClass);
            Assert.Equal(ServiceLifetime.Transient, transientClass.Lifetime);

            var scpoedClass = Services.FirstOrDefault(service => service.ImplementationType == typeof(ScopedClass));

            Assert.NotNull(scpoedClass);
            Assert.Equal(ServiceLifetime.Scoped, scpoedClass.Lifetime);

            var injectServiceCount = Services.Count(service => service.ImplementationType == typeof(SinglethonClass));

            Assert.Equal(1, injectServiceCount);
        }

        [Fact]
        public void InheritInterface_HasTwoInterface_ShouldOnlyOneCount()
        {
            var injectServiceCount = Services.Count(service =>
                                    service.ImplementationType == typeof(HasTwoInterfaceClass));

            Assert.Equal(1, injectServiceCount);
        }


        [Fact]
        public void HasAttribute_ShouldHasItSelf()
        {
            var injectServiceCount = Services.Count(service =>
                                    service.ImplementationType == typeof(DefaultAttributeClass));

            Assert.Equal(1, injectServiceCount);
        }

        [Fact]
        public void HasAttribute_ShouldNotHasItSelf()
        {
            var injectServiceCount = Services.Count(service =>
                                    service.ImplementationType == typeof(NotIncludeItSelfAttributeClass));

            Assert.Equal(0, injectServiceCount);
        }

        [Fact]
        public void HasAttribute_ShouldHasRightLifetime()
        {
            var singlethonClass = Services.FirstOrDefault(service =>
                                    service.ImplementationType == typeof(SinglethonAttributeClass));

            Assert.Equal(ServiceLifetime.Singleton, singlethonClass.Lifetime);

            var scpoedClass = Services.FirstOrDefault(service =>
                        service.ImplementationType == typeof(ScopedAttributeClass));

            Assert.Equal(ServiceLifetime.Scoped, scpoedClass.Lifetime);

            var defaultClass = Services.FirstOrDefault(service =>
                        service.ImplementationType == typeof(DefaultAttributeClass));

            Assert.Equal(ServiceLifetime.Transient, defaultClass.Lifetime);
        }

        [Fact]
        public void HasAttribute_ShouldMoreServices()
        {
            var injectServiceCount = Services.Count(service =>
                                    service.ImplementationType == typeof(HasMoreServiceAttributeClass));

            Assert.Equal(4, injectServiceCount);
        }

        [Fact]
        public void HasTwoFeature_ShouldOnlyOneService()
        {
            var injectServiceCount = Services.Count(service =>
                                                service.ImplementationType == typeof(TwoFeatureFake));

            Assert.Equal(1, injectServiceCount);
        }

        public static IServiceCollection BuildServiceCollection()
        {
            IServiceCollection Services = new ServiceCollection();

            return Services;
        }

        public static IMiCakeModuleCollection BuildCurrentModule()
        {
            IMiCakeModuleCollection miCakeModules = new MiCakeModuleCollection();

            var moduleType = typeof(CurrentMiCakeModule);

            miCakeModules.Add(new MiCakeModuleDescriptor(moduleType, (MiCakeModule)Activator.CreateInstance(moduleType)));

            return miCakeModules;
        }
    }
}
