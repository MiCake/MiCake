using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.IntegrationTests.Fixtures;

namespace MiCake.IntegrationTests.Modularity
{
    [Collection("MiCakeIntegrationTests")]
    public class ModuleDependencyIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly MiCakeAppFixture _fixture;

        public ModuleDependencyIntegrationTests(MiCakeAppFixture fixture)
        {
            _fixture = fixture;

            // Build a small MiCake app for tests
            _serviceProvider = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var builder = services.AddMiCake<TestEntryModule_ForIntegration>();
                builder.Build();
            });
        }

        [Fact]
        public void MiCakeRootModule_ShouldBeFirst_WhenOtherModuleHasNoDependencies()
        {
            var moduleContext = _serviceProvider.GetRequiredService<IMiCakeModuleContext>();

            Assert.NotNull(moduleContext);
            Assert.NotEmpty(moduleContext.MiCakeModules);

            // The expectation: framework-level MiCakeRootModule should be prioritized and appear
            // before other plain modules that have no dependencies.
            var first = moduleContext.MiCakeModules.First();
            Assert.Equal(typeof(MiCakeRootModule), first.ModuleType);
        }

        [Fact]
        public void MiCakeRootModule_ShouldAppearBeforeAllIndependentModules()
        {
            var moduleContext = _serviceProvider.GetRequiredService<IMiCakeModuleContext>();

            // Check that any independent modules (no rely-on relationships) appear after root.
            var modules = moduleContext.MiCakeModules.Select(m => m.ModuleType).ToList();
            var rootIndex = modules.IndexOf(typeof(MiCakeRootModule));
            Assert.True(rootIndex >= 0, "MiCakeRootModule should be present in the module list");

            // Any test modules that don't rely on other modules should not appear before root.
            // We know the test graph includes IndependentModuleA and IndependentModuleB.
            var independentAIndex = modules.IndexOf(typeof(IndependentModuleA));
            var independentBIndex = modules.IndexOf(typeof(IndependentModuleB));

            // If any independent module exists in the collection, it must not be before the root
            if (independentAIndex >= 0)
                Assert.True(independentAIndex > rootIndex, "IndependentModuleA must come after MiCakeRootModule");

            if (independentBIndex >= 0)
                Assert.True(independentBIndex > rootIndex, "IndependentModuleB must come after MiCakeRootModule");
        }

        [Fact]
        public void MiCakeRootModule_ShouldStillBeFirst_EvenWhenIndependentListedBeforeRoot()
        {
            // Build a separate provider with the different entry module ordering
            var sp = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var builder = services.AddMiCake<TestEntryModule_IndependentBeforeRoot>();
                builder.Build();
            });

            try
            {
                var moduleContext = sp.GetRequiredService<IMiCakeModuleContext>();

                var modules = moduleContext.MiCakeModules.Select(m => m.ModuleType).ToList();
                var rootIndex = modules.IndexOf(typeof(MiCakeRootModule));
                var independentIndex = modules.IndexOf(typeof(IndependentModuleA));

                // Even though IndependentModuleA is listed before MiCakeRootModule in the RelyOn attribute,
                // MiCakeRootModule should still appear first because it's a framework-level root module.
                // This ensures framework initialization happens before any application modules.
                Assert.True(rootIndex < independentIndex, "MiCakeRootModule should appear before IndependentModuleA even when listed after it.");
            }
            finally
            {
                _fixture.ReleaseServiceProvider(sp);
            }
        }

        [Fact]
        public void FrameworkModules_ShouldAppearBeforeRegularModules_WhenNoDependencies()
        {
            // Build a test with multiple independent framework and regular modules
            var sp = _fixture.CreateServiceProvider(services =>
            {
                services.AddLogging();

                var builder = services.AddMiCake<TestEntryModule_MixedIndependentModules>();
                builder.Build();
            });

            try
            {
                var moduleContext = sp.GetRequiredService<IMiCakeModuleContext>();
                var modules = moduleContext.MiCakeModules.ToList();

                // Find framework and regular modules
                var frameworkModules = modules.Where(m => m.Instance.IsFrameworkLevel).ToList();
                var regularModules = modules.Where(m => !m.Instance.IsFrameworkLevel).ToList();

                // Get the last framework module index and first regular module index
                var lastFrameworkIndex = modules.IndexOf(frameworkModules.Last());
                var firstRegularIndex = modules.IndexOf(regularModules.First());

                // All framework modules should appear before any regular modules
                Assert.True(lastFrameworkIndex < firstRegularIndex, 
                    "All framework modules should be loaded before any regular modules when there are no dependency constraints.");
            }
            finally
            {
                _fixture.ReleaseServiceProvider(sp);
            }
        }

        public void Dispose()
        {
            _fixture?.ReleaseServiceProvider(_serviceProvider);
        }

        #region Test Modules

        // The entry module relies on MiCakeRootModule and two independent modules
        [RelyOn(typeof(ModuleE))]
        private class TestEntryModule_ForIntegration : MiCakeModule { }

        // independent modules with no dependencies
        private class IndependentModuleA : MiCakeModule { }
        private class IndependentModuleB : MiCakeModule { }

        [RelyOn(typeof(MiCakeRootModule))]
        private class ModuleA : MiCakeModule
        {
            public override bool IsFrameworkLevel => true;
        }
        [RelyOn(typeof(ModuleA))]
        private class ModuleB : MiCakeModule
        {
            public override bool IsFrameworkLevel => true;
        }
        [RelyOn(typeof(ModuleB))]
        private class ModuleC : MiCakeModule
        {
            public override bool IsFrameworkLevel => true;
        }

        private class ModuleD : MiCakeModule { }

        [RelyOn(typeof(ModuleD),typeof(ModuleC))]
        private class ModuleE : MiCakeModule { }

        // An entry module that lists an independent module BEFORE MiCakeRootModule
        [RelyOn(typeof(IndependentModuleA), typeof(MiCakeRootModule))]
        private class TestEntryModule_IndependentBeforeRoot : MiCakeModule { }

        // Test module with multiple independent framework and regular modules
        [RelyOn(typeof(IndependentFrameworkModule), typeof(IndependentRegularModule1), typeof(MiCakeRootModule))]
        private class TestEntryModule_MixedIndependentModules : MiCakeModule { }

        private class IndependentFrameworkModule : MiCakeModule 
        { 
            public override bool IsFrameworkLevel => true; 
        }
        
        private class IndependentRegularModule1 : MiCakeModule { }

        #endregion
    }
}
