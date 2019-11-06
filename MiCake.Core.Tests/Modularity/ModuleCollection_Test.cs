using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Modularity;
using MiCake.Core.Tests.Modularity.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MiCake.Core.Tests.Modularity
{
    public class ModuleCollection_Test
    {
        [Fact]
        public void ModuleCollectionGetAllAssemblyTest()
        {
            var engine = CreateMiCakeModuleEngine();
            engine.LoadMiCakeModules(typeof(StarpUpModule));
            var ass = engine.MiCakeModules.GetAllReferAssembly();

            Assert.Single(ass);
        }

        private IMiCakeModuleEngine CreateMiCakeModuleEngine()
        {
            var servers = new Mock<IServiceCollection>();
            var logger = new Mock<ILogger<MiCakeModuleEngine>>();

            IMiCakeModuleEngine miCakeModuleEngine = new DefaultMiCakeModuleEngine(servers.Object, logger.Object);

            return miCakeModuleEngine;
        }
    }
}
