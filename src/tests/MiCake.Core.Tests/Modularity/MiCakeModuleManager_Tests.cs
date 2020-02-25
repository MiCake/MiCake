using MiCake.Core.Modularity;
using MiCake.Core.Tests.Modularity.Fakes;
using System;
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

            var allmodules = moduleManager.AllModules;
            Assert.Equal(1, allmodules.Count);

            var defaultModules = moduleManager.MiCakeModules;
            Assert.Equal(1, defaultModules.Count);

            var featureModules = moduleManager.FeatureModules;
            Assert.Equal(0, featureModules.Count);
        }

        [Fact]
        public void HasDepency_ShouldLoadDepencyModules()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.PopulateModules(typeof(StartUpModule));

            var allmodules = moduleManager.AllModules;
            Assert.Equal(3, allmodules.Count);

            var defaultModules = moduleManager.MiCakeModules;
            Assert.Equal(3, defaultModules.Count);

            var featureModules = moduleManager.FeatureModules;
            Assert.Equal(0, featureModules.Count);

            //right order
            var firstModule = moduleManager.MiCakeModules.FirstOrDefault();
            Assert.NotNull(firstModule);
            Assert.Equal(typeof(DepencyModuleA), firstModule.Type);

            var lastModule = moduleManager.MiCakeModules.LastOrDefault();
            Assert.NotNull(lastModule);
            Assert.Equal(typeof(StartUpModule), lastModule.Type);
        }

        [Fact]
        public void HasFeature_ShouldLoadAddedFeatureModule()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.AddFeatureModule(typeof(FeatureModuleA));
            moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));

            var allmodules = moduleManager.AllModules;
            Assert.Equal(2, allmodules.Count);

            var defaultModules = moduleManager.MiCakeModules;
            Assert.Equal(1, defaultModules.Count);

            var featureModules = moduleManager.FeatureModules;
            Assert.Equal(1, featureModules.Count);
        }

        [Fact]
        public void OnlyMiCakeModule_ShouldErrorInCheckFeatureModule()
        {
            var moduleManager = new MiCakeModuleManager();
            Assert.ThrowsAny<Exception>(() => moduleManager.AddFeatureModule(typeof(StartUpModule)));
        }

        [Fact]
        public void OnlyHasFeature_ShouldCorrectOrder()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.AddFeatureModule(typeof(FeatureModuleA));
            moduleManager.AddFeatureModule(typeof(FeatureModuleBDepencyModuleA));
            moduleManager.AddFeatureModule(typeof(FeatureModuleCDencyModuleB));
            moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));

            var allmodules = moduleManager.AllModules;
            Assert.Equal(4, allmodules.Count);

            var defaultModules = moduleManager.MiCakeModules;
            Assert.Equal(1, defaultModules.Count);

            var featureModules = moduleManager.FeatureModules;
            Assert.Equal(3, featureModules.Count);

            //order
            var firstModule = moduleManager.FeatureModules.FirstOrDefault();
            Assert.NotNull(firstModule);
            Assert.Equal(typeof(FeatureModuleA), firstModule.Type);

            var lastModule = moduleManager.FeatureModules.LastOrDefault();
            Assert.NotNull(lastModule);
            Assert.Equal(typeof(FeatureModuleCDencyModuleB), lastModule.Type);
        }

        [Fact]
        public void DeafultAndFeature_ShouldCorrectOrder()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.AddFeatureModule(typeof(FeatureModuleA));
            moduleManager.AddFeatureModule(typeof(FeatureModuleBDepencyModuleA));
            moduleManager.AddFeatureModule(typeof(FeatureModuleCDencyModuleB));
            moduleManager.PopulateModules(typeof(StartUpModule));

            var allmodules = moduleManager.AllModules;
            Assert.Equal(6, allmodules.Count);

            var defaultModules = moduleManager.MiCakeModules;
            Assert.Equal(3, defaultModules.Count);

            var featureModules = moduleManager.FeatureModules;
            Assert.Equal(3, featureModules.Count);

            //order
            var firstModule = moduleManager.AllModules.FirstOrDefault();
            Assert.NotNull(firstModule);
            Assert.Equal(typeof(DepencyModuleA), firstModule.Type);

            var lastModule = moduleManager.AllModules.LastOrDefault();
            Assert.NotNull(lastModule);
            Assert.Equal(typeof(FeatureModuleCDencyModuleB), lastModule.Type);
        }
    }
}
