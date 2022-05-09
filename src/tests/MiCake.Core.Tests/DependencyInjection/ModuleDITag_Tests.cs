
using System;


namespace MiCake.Core.Tests.DependencyInjection
{
    public class ModuleDITag_Tests
    {
        public IServiceCollection Services { get; }

        public ModuleDITag_Tests()
        {
            Services = BuildServiceCollection();
        }

        [Fact]
        public void UseAutoDI_ShouldRegisterCurrentModuleService()
        {
            DefaultServiceRegistrar defaultServiceRegistrar = new(Services);
            defaultServiceRegistrar.Register(BuildCurrentModule(typeof(CurrentMiCakeModule)));

            Assert.NotEmpty(Services);
        }

        [Fact]
        public void DisableAutoDI_ShouldNotRegisterCurrentModuleService()
        {
            DefaultServiceRegistrar defaultServiceRegistrar = new(Services);
            defaultServiceRegistrar.Register(BuildCurrentModule(typeof(CurrentDisableDIMiCakeModule)));

            Assert.Empty(Services);
        }

        [Fact]
        public void TwoAutoDITag_ShouldNotRegisterCurrentModuleService()
        {
            DefaultServiceRegistrar defaultServiceRegistrar = new(Services);
            defaultServiceRegistrar.Register(BuildCurrentModule(typeof(BothTwoDITagMiCakeModule)));

            Assert.Empty(Services);
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
