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
            var moduleContext = moduleManager.ModuleContext;

            var allmodules = moduleContext.AllModules;
            Assert.Single(allmodules);

            var defaultModules = moduleContext.MiCakeModules;
            Assert.Single(defaultModules);

            var featureModules = moduleContext.FeatureModules;
            Assert.Empty(featureModules);
        }

        [Fact]
        public void HasDepency_ShouldLoadDepencyModules()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.PopulateModules(typeof(StartUpModule));
            var moduleContext = moduleManager.ModuleContext;

            var allmodules = moduleContext.AllModules;
            Assert.Equal(3, allmodules.Count);

            var defaultModules = moduleContext.MiCakeModules;
            Assert.Equal(3, defaultModules.Count);

            var featureModules = moduleContext.FeatureModules;
            Assert.Empty(featureModules);

            //right order
            var firstModule = moduleContext.MiCakeModules.FirstOrDefault();
            Assert.NotNull(firstModule);
            Assert.Equal(typeof(DepencyModuleA), firstModule.ModuleType);

            var lastModule = moduleContext.MiCakeModules.LastOrDefault();
            Assert.NotNull(lastModule);
            Assert.Equal(typeof(StartUpModule), lastModule.ModuleType);
        }

        [Fact]
        public void HasFeature_ShouldLoadAddedFeatureModule()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.AddFeatureModule(typeof(FeatureModuleA));
            moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));
            var moduleContext = moduleManager.ModuleContext;

            var allmodules = moduleContext.AllModules;
            Assert.Equal(2, allmodules.Count);

            var defaultModules = moduleContext.MiCakeModules;
            Assert.Single(defaultModules);

            var featureModules = moduleContext.FeatureModules;
            Assert.Single(featureModules);
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
            var moduleContext = moduleManager.ModuleContext;

            var allmodules = moduleContext.AllModules;
            Assert.Equal(4, allmodules.Count);

            var defaultModules = moduleContext.MiCakeModules;
            Assert.Single(defaultModules);

            var featureModules = moduleContext.FeatureModules;
            Assert.Equal(3, featureModules.Count);

            //order
            var firstModule = moduleContext.FeatureModules.FirstOrDefault();
            Assert.NotNull(firstModule);
            Assert.Equal(typeof(FeatureModuleA), firstModule.ModuleType);

            var lastModule = moduleContext.FeatureModules.LastOrDefault();
            Assert.NotNull(lastModule);
            Assert.Equal(typeof(FeatureModuleCDencyModuleB), lastModule.ModuleType);
        }

        [Fact]
        public void DeafultAndFeature_ShouldCorrectOrder()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.AddFeatureModule(typeof(FeatureModuleA));
            moduleManager.AddFeatureModule(typeof(FeatureModuleBDepencyModuleA));
            moduleManager.AddFeatureModule(typeof(FeatureModuleCDencyModuleB));
            moduleManager.PopulateModules(typeof(StartUpModule));
            var moduleContext = moduleManager.ModuleContext;

            var allmodules = moduleContext.AllModules;
            Assert.Equal(6, allmodules.Count);

            var defaultModules = moduleContext.MiCakeModules;
            Assert.Equal(3, defaultModules.Count);

            var featureModules = moduleContext.FeatureModules;
            Assert.Equal(3, featureModules.Count);

            //order
            var firstModule = moduleContext.AllModules.FirstOrDefault();
            Assert.NotNull(firstModule);
            Assert.Equal(typeof(DepencyModuleA), firstModule.ModuleType);

            var lastModule = moduleContext.AllModules.LastOrDefault();
            Assert.NotNull(lastModule);
            Assert.Equal(typeof(FeatureModuleCDencyModuleB), lastModule.ModuleType);
        }
    }
}
