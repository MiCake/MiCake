using MiCake.Core.Tests.Modularity.Fakes;
using MiCake.Core.Util.Collections;
using System;
using System.Linq;

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
            Assert.Equal(1, allmodules.Count);
        }

        [Fact]
        public void HasDepency_ShouldLoadDepencyModules()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.PopulateModules(typeof(StartUpModule));
            var moduleContext = moduleManager.ModuleContext;

            var allmodules = moduleContext.MiCakeModules;
            Assert.Equal(3, allmodules.Count);

            //right order
            var firstModule = moduleContext.MiCakeModules.FirstOrDefault();
            Assert.NotNull(firstModule);
            Assert.Equal(typeof(DepencyModuleA), firstModule.ModuleType);

            var lastModule = moduleContext.MiCakeModules.LastOrDefault();
            Assert.NotNull(lastModule);
            Assert.Equal(typeof(StartUpModule), lastModule.ModuleType);
        }

        [Fact]
        public void SlotANoDepencyModule_ShouldInculeModule()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.Slot<DepencyModuleA>();
            moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));

            var moduleContext = moduleManager.ModuleContext;
            Assert.Equal(2, moduleContext.MiCakeModules.Count);
        }

        [Fact]
        public void SlotAHasDepencyModule_ShouldInculeModule()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.Slot<DepencyModuleB>();
            moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));

            var moduleContext = moduleManager.ModuleContext;
            Assert.Equal(3, moduleContext.MiCakeModules.Count);  // B is rely on A.
        }

        [Fact]
        public void SlotACoreModule_ModuleDescriptHasFlag()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.Slot(typeof(CoreAModule));
            moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));

            var coreModules = moduleManager.ModuleContext.MiCakeModules.Where(s => s.IsCoreModule).ToList();
            Assert.Single(coreModules);
        }

        [Fact]
        public void SlotNoMiCakeModule_ShouldThrowAException()
        {
            var moduleManager = new MiCakeModuleManager();
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                moduleManager.Slot(typeof(NoMiCakeModule));
            });
        }

        [Fact]
        public void CallMulitPoplulate_ShouldThrowAException()
        {
            var moduleManager = new MiCakeModuleManager();
            moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));
            Assert.ThrowsAny<InvalidOperationException>(() =>
            {
                moduleManager.PopulateModules(typeof(StartUpModule_NoDenpency));
            });
        }

        [Fact]
        public void UseCusotomSorter_ShouldChangeModuleOrder()
        {
            // no sorter.
            var moduleManager1 = new MiCakeModuleManager();
            moduleManager1.Slot<CoreAModule>();
            moduleManager1.PopulateModules(typeof(StartUpModule));

            Assert.False(moduleManager1.ModuleContext.MiCakeModules[^1].IsCoreModule);

            // use sorter.
            var moduleManager = new MiCakeModuleManager();
            moduleManager.Slot<CoreAModule>();
            moduleManager.PopulateModules(typeof(StartUpModule), new CustomModuleSorter());

            Assert.True(moduleManager.ModuleContext.MiCakeModules[^1].IsCoreModule);
        }

        internal class CustomModuleSorter : IMiCakeModuleSorter
        {
            public IMiCakeModuleCollection Sort(IMiCakeModuleCollection ordinalModules)
            {
                return ordinalModules.ExchangeOrder(s =>
                 {
                     return s.IsCoreModule;
                 }, ordinalModules.Count - 1).ToMiCakeModuleCollection();
            }
        }
    }
}
