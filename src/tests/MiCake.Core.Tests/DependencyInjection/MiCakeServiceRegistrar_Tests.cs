using MiCake.Core.Tests.DependencyInjection.Fakes;
using System;
using System.Linq;

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
            var singlethonClass = Services.Where(service => service.ImplementationType == typeof(SinglethonClass)).FirstOrDefault();

            Assert.NotNull(singlethonClass);
            Assert.Equal(ServiceLifetime.Singleton, singlethonClass.Lifetime);

            var transientClass = Services.Where(service => service.ImplementationType == typeof(TransientClass)).FirstOrDefault();

            Assert.NotNull(transientClass);
            Assert.Equal(ServiceLifetime.Transient, transientClass.Lifetime);

            var scpoedClass = Services.Where(service => service.ImplementationType == typeof(ScopedClass)).FirstOrDefault();

            Assert.NotNull(scpoedClass);
            Assert.Equal(ServiceLifetime.Scoped, scpoedClass.Lifetime);

            var injectServiceCount = Services.Where(service => service.ImplementationType == typeof(SinglethonClass)).Count();

            Assert.Equal(1, injectServiceCount);
        }

        [Fact]
        public void InheritInterface_HasTwoInterface_ShouldOnlyOneCount()
        {
            var injectServiceCount = Services.Where(service =>
                                    service.ImplementationType == typeof(HasTwoInterfaceClass)).Count();

            Assert.Equal(1, injectServiceCount);
        }


        [Fact]
        public void HasAttribute_ShouldHasItSelf()
        {
            var injectServiceCount = Services.Where(service =>
                                    service.ImplementationType == typeof(DefaultAttributeClass)).Count();

            Assert.Equal(1, injectServiceCount);
        }

        [Fact]
        public void HasAttribute_ShouldNotHasItSelf()
        {
            var injectServiceCount = Services.Where(service =>
                                    service.ImplementationType == typeof(NotIncludeItSelfAttributeClass)).Count();

            Assert.Equal(0, injectServiceCount);
        }

        [Fact]
        public void HasAttribute_ShouldHasRightLifetime()
        {
            var singlethonClass = Services.Where(service =>
                                    service.ImplementationType == typeof(SinglethonAttributeClass)).FirstOrDefault();

            Assert.Equal(ServiceLifetime.Singleton, singlethonClass.Lifetime);

            var scpoedClass = Services.Where(service =>
                        service.ImplementationType == typeof(ScopedAttributeClass)).FirstOrDefault();

            Assert.Equal(ServiceLifetime.Scoped, scpoedClass.Lifetime);

            var defaultClass = Services.Where(service =>
                        service.ImplementationType == typeof(DefaultAttributeClass)).FirstOrDefault();

            Assert.Equal(ServiceLifetime.Transient, defaultClass.Lifetime);
        }

        [Fact]
        public void HasAttribute_ShouldMoreServices()
        {
            var injectServiceCount = Services.Where(service =>
                                    service.ImplementationType == typeof(HasMoreServiceAttributeClass)).Count();

            Assert.Equal(4, injectServiceCount);
        }

        [Fact]
        public void HasTwoFeature_ShouldOnlyOneService()
        {
            var injectServiceCount = Services.Where(service =>
                                                service.ImplementationType == typeof(TwoFeatureFake)).Count();

            Assert.Equal(1, injectServiceCount);
        }

        public IServiceCollection BuildServiceCollection()
        {
            IServiceCollection Services = new ServiceCollection();

            return Services;
        }

        public IMiCakeModuleCollection BuildCurrentModule()
        {
            IMiCakeModuleCollection miCakeModules = new MiCakeModuleCollection();

            var moduleType = typeof(CurrentMiCakeModule);

            miCakeModules.Add(new MiCakeModuleDescriptor(moduleType, (MiCakeModule)Activator.CreateInstance(moduleType)));

            return miCakeModules;
        }
    }
}
