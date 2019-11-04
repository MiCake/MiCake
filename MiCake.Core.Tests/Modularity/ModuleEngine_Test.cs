using Castle.Core.Logging;
using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Modularity;
using MiCake.Core.Tests.Modularity.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MiCake.Core.Tests.Modularity
{
    public class ModuleEngine_Test
    {

        [Fact]
        public void MiCakeModuleEngineLoad()
        {
            var engine = CreateMiCakeModuleEngine();

            var reslut = engine.LoadMiCakeModules(typeof(StarpUpModule));

            Assert.Contains(reslut, s => s.Type == typeof(StarpUpModule));

        }

        [Fact]
        public void MiCakeModuleEngineLoad_HasNoMiCakeModule()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var engine = CreateMiCakeModuleEngine();
                var reslut = engine.LoadMiCakeModules(typeof(StarupModule_NoMiCakeModule));
            });

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
