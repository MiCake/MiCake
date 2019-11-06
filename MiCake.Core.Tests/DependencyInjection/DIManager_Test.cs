using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.DependencyInjection;
using MiCake.Core.Modularity;
using MiCake.Core.Tests.DependencyInjection.Fakes;
using MiCake.Core.Tests.Modularity.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MiCake.Core.Tests.DependencyInjection
{
    public class DIManager_Test
    {
        [Fact]
        public void DIPopulateAutoInterfaceServiceTest()
        {
            var services = new ServiceCollection();

            var defaultDIManager = new DefaultMiCakeDIManager(services);
            defaultDIManager.PopulateAutoService(CreateMiCakeModuleEngine().MiCakeModules);

            IDIContainer dIContainer = new DefaultDIContainer(services);

            var instance = (AutoInjectSingletonClass)dIContainer.GetService<AutoInjectSingletonClass>();
            var insStr = instance.BackString();

            Assert.NotNull(instance);
            Assert.Equal("this is SingletonClass", insStr);
        }

        [Fact]
        public void DIPopulateAutoAttributeServiceTest()
        {
            var services = new ServiceCollection();

            var defaultDIManager = new DefaultMiCakeDIManager(services);
            defaultDIManager.PopulateAutoService(CreateMiCakeModuleEngine().MiCakeModules);

            IDIContainer dIContainer = new DefaultDIContainer(services);

            var instance = (AutoInjectWithAttributeClass)dIContainer.GetService<AutoInjectWithAttributeClass>();
            var insStr = instance.BackString();

            Assert.NotNull(instance);
            Assert.Equal("this is Attribute Auto Inject", insStr);
        }

        [Fact]
        public void DIPopulateBothAttributeAndInerfaceServiceTest()
        {
            var services = new ServiceCollection();

            var defaultDIManager = new DefaultMiCakeDIManager(services);
            defaultDIManager.PopulateAutoService(CreateMiCakeModuleEngine().MiCakeModules);

            IDIContainer dIContainer = new DefaultDIContainer(services);

            var instance = (BothAttributeAndInterfaceClass)dIContainer.GetService<BothAttributeAndInterfaceClass>();
            var insStr = instance.BackString();

            Assert.NotNull(instance);
            Assert.Equal("this is Both Attribute And Interfac  Class", insStr);
        }

        private IMiCakeModuleEngine CreateMiCakeModuleEngine()
        {
            var servers = new Mock<IServiceCollection>();
            var logger = new Mock<ILogger<MiCakeModuleEngine>>();

            IMiCakeModuleEngine miCakeModuleEngine = new DefaultMiCakeModuleEngine(servers.Object, logger.Object);
            miCakeModuleEngine.LoadMiCakeModules(typeof(StarpUpModule));

            return miCakeModuleEngine;
        }
    }
}
