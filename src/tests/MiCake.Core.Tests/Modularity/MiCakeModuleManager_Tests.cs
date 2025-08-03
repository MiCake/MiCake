using MiCake.Core.Modularity;
using MiCake.Core.Tests.Modularity.Fakes;
using System.Linq;
using Xunit;

namespace MiCake.Core.Tests.Modularity
{
    public class MiCakeModuleManager_Tests
    {
        public MiCakeModuleManager_Tests()
        {
        }

        [Fact]
        public void NoDepency_ShouldOnlyItSelf()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));
            var moduleContext = moduleManager.ModuleContext;

            var allmodules = moduleContext.MiCakeModules;
            Assert.Single(allmodules);

            var defaultModules = moduleContext.MiCakeModules;
            Assert.Single(defaultModules);
        }

        [Fact]
        public void HasDepency_ShouldLoadDepencyModules()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.PopulateModules(typeof(StartUpModule));
            var moduleContext = moduleManager.ModuleContext;

            var allmodules = moduleContext.MiCakeModules;
            Assert.Equal(3, allmodules.Count);

            var defaultModules = moduleContext.MiCakeModules;
            Assert.Equal(3, defaultModules.Count);

            //right order
            var firstModule = moduleContext.MiCakeModules.FirstOrDefault();
            Assert.NotNull(firstModule);
            Assert.Equal(typeof(DepencyModuleA), firstModule.ModuleType);

            var lastModule = moduleContext.MiCakeModules.LastOrDefault();
            Assert.NotNull(lastModule);
            Assert.Equal(typeof(StartUpModule), lastModule.ModuleType);
        }
    }
}
