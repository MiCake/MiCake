using MiCake.Core.Modularity;
using System;
using System.Linq;
using Xunit;

namespace MiCake.Core.Tests.Modularity
{
    /// <summary>
    /// Tests for ModuleDependencyResolver using Kahn's algorithm for topological sorting.
    /// </summary>
    public class ModuleDependencyResolver_Tests
    {
        #region Basic Dependency Resolution

        [Fact]
        public void ResolveLoadOrder_SingleModule_ShouldReturnSingleModule()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();
            var moduleA = new TestModuleA();
            var descriptorA = new MiCakeModuleDescriptor(typeof(TestModuleA), moduleA);
            resolver.RegisterModule(descriptorA);

            // Act
            var result = resolver.ResolveLoadOrder();

            // Assert
            Assert.Single(result);
            Assert.Equal(typeof(TestModuleA), result[0].ModuleType);
        }

        [Fact]
        public void ResolveLoadOrder_TwoModulesNoDependency_ShouldReturnBothModules()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();
            var moduleA = new TestModuleA();
            var moduleB = new TestModuleB();
            var descriptorA = new MiCakeModuleDescriptor(typeof(TestModuleA), moduleA);
            var descriptorB = new MiCakeModuleDescriptor(typeof(TestModuleB), moduleB);
            
            resolver.RegisterModule(descriptorA);
            resolver.RegisterModule(descriptorB);

            // Act
            var result = resolver.ResolveLoadOrder();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, d => d.ModuleType == typeof(TestModuleA));
            Assert.Contains(result, d => d.ModuleType == typeof(TestModuleB));
        }

        #endregion

        #region Linear Dependency Chain

        [Fact]
        public void ResolveLoadOrder_LinearDependencyChain_ShouldResolveInCorrectOrder()
        {
            // Arrange: C depends on B, B depends on A (linear chain)
            var resolver = new ModuleDependencyResolver();
            var moduleA = new TestModuleA();
            var moduleB = new TestModuleBDependsOnA();
            var moduleC = new TestModuleCDependsOnB();
            
            var descriptorA = new MiCakeModuleDescriptor(typeof(TestModuleA), moduleA);
            var descriptorB = new MiCakeModuleDescriptor(typeof(TestModuleBDependsOnA), moduleB);
            var descriptorC = new MiCakeModuleDescriptor(typeof(TestModuleCDependsOnB), moduleC);
            
            resolver.RegisterModule(descriptorC);
            resolver.RegisterModule(descriptorB);
            resolver.RegisterModule(descriptorA);

            // Act
            var result = resolver.ResolveLoadOrder();

            // Assert
            Assert.Equal(3, result.Count);
            var indexA = result.FindIndex(d => d.ModuleType == typeof(TestModuleA));
            var indexB = result.FindIndex(d => d.ModuleType == typeof(TestModuleBDependsOnA));
            var indexC = result.FindIndex(d => d.ModuleType == typeof(TestModuleCDependsOnB));
            
            // A should load before B, B should load before C (linear chain)
            Assert.True(indexA < indexB, "ModuleA should load before ModuleB");
            Assert.True(indexB < indexC, "ModuleB should load before ModuleC");
        }

        #endregion

        #region Multiple Dependencies

        [Fact]
        public void ResolveLoadOrder_MultipleDirectDependencies_ShouldResolveCorrectly()
        {
            // Arrange: D depends on both B and C, B and C depend on A
            var resolver = new ModuleDependencyResolver();
            var moduleA = new TestModuleA();
            var moduleB = new TestModuleB();
            var moduleC = new TestModuleC();
            var moduleD = new TestModuleD();
            
            var descriptorA = new MiCakeModuleDescriptor(typeof(TestModuleA), moduleA);
            var descriptorB = new MiCakeModuleDescriptor(typeof(TestModuleB), moduleB);
            var descriptorC = new MiCakeModuleDescriptor(typeof(TestModuleC), moduleC);
            var descriptorD = new MiCakeModuleDescriptor(typeof(TestModuleD), moduleD);
            
            resolver.RegisterModule(descriptorD);
            resolver.RegisterModule(descriptorC);
            resolver.RegisterModule(descriptorB);
            resolver.RegisterModule(descriptorA);

            // Act
            var result = resolver.ResolveLoadOrder();

            // Assert
            Assert.Equal(4, result.Count);
            var indexA = result.FindIndex(d => d.ModuleType == typeof(TestModuleA));
            var indexB = result.FindIndex(d => d.ModuleType == typeof(TestModuleB));
            var indexC = result.FindIndex(d => d.ModuleType == typeof(TestModuleC));
            var indexD = result.FindIndex(d => d.ModuleType == typeof(TestModuleD));
            
            // A should load first
            Assert.Equal(0, indexA);
            // B and C should load after A
            Assert.True(indexB > indexA && indexC > indexA, "B and C should load after A");
            // D should load last
            Assert.Equal(3, indexD);
        }

        #endregion

        #region Circular Dependency Detection

        [Fact]
        public void ResolveLoadOrder_CircularDependency_ShouldThrowException()
        {
            // Arrange: Create circular dependency manually
            var resolver = new ModuleDependencyResolver();
            var moduleCircularA = new TestCircularModuleA();
            var moduleCircularB = new TestCircularModuleB();
            
            var descriptorA = new MiCakeModuleDescriptor(typeof(TestCircularModuleA), moduleCircularA);
            var descriptorB = new MiCakeModuleDescriptor(typeof(TestCircularModuleB), moduleCircularB);
            
            resolver.RegisterModule(descriptorA);
            resolver.RegisterModule(descriptorB);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => resolver.ResolveLoadOrder());
            Assert.Contains("Circular module dependency detected", exception.Message);
            Assert.Contains("TestCircularModuleA", exception.Message);
            Assert.Contains("TestCircularModuleB", exception.Message);
        }

        #endregion

        #region Complex Dependency Graph

        [Fact]
        public void ResolveLoadOrder_ComplexDependencyGraph_ShouldResolveCorrectly()
        {
            // Arrange: Complex graph
            //     A
            //    / \
            //   B   C
            //    \ / \
            //     D   E
            //      \ /
            //       F
            var resolver = new ModuleDependencyResolver();
            var moduleA = new TestModuleA();
            var moduleB = new TestModuleB();
            var moduleC = new TestModuleC();
            var moduleD = new TestModuleD();
            var moduleE = new TestModuleE();
            var moduleF = new TestModuleF();
            
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), moduleA));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleB), moduleB));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleC), moduleC));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleD), moduleD));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleE), moduleE));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleF), moduleF));

            // Act
            var result = resolver.ResolveLoadOrder();

            // Assert
            Assert.Equal(6, result.Count);
            var indices = result.Select((d, i) => new { Type = d.ModuleType, Index = i })
                                .ToDictionary(x => x.Type, x => x.Index);
            
            // Verify dependency order constraints
            Assert.True(indices[typeof(TestModuleA)] < indices[typeof(TestModuleB)], "A before B");
            Assert.True(indices[typeof(TestModuleA)] < indices[typeof(TestModuleC)], "A before C");
            Assert.True(indices[typeof(TestModuleB)] < indices[typeof(TestModuleD)], "B before D");
            Assert.True(indices[typeof(TestModuleC)] < indices[typeof(TestModuleD)], "C before D");
            Assert.True(indices[typeof(TestModuleC)] < indices[typeof(TestModuleE)], "C before E");
            Assert.True(indices[typeof(TestModuleD)] < indices[typeof(TestModuleF)], "D before F");
            Assert.True(indices[typeof(TestModuleE)] < indices[typeof(TestModuleF)], "E before F");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ResolveLoadOrder_EmptyResolver_ShouldReturnEmptyList()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();

            // Act
            var result = resolver.ResolveLoadOrder();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void RegisterModule_NullDescriptor_ShouldThrowArgumentNullException()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => resolver.RegisterModule(null));
        }

        #endregion

        #region GetDependencyGraph Tests

        [Fact]
        public void GetDependencyGraph_ShouldMatchResolveLoadOrder()
        {
            // Arrange: Create a complex dependency graph
            var resolver = new ModuleDependencyResolver();
            var moduleA = new TestModuleA();
            var moduleB = new TestModuleBDependsOnA();
            var moduleC = new TestModuleCDependsOnB();
            
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), moduleA));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleBDependsOnA), moduleB));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleCDependsOnB), moduleC));

            // Act
            var loadOrder = resolver.ResolveLoadOrder();
            var dependencyGraph = resolver.GetDependencyGraph();

            // Assert: The dependency graph should match the load order
            var expectedGraph = string.Join(" -> ", loadOrder.Select(d => d.ModuleType.Name));
            Assert.Equal(expectedGraph, dependencyGraph);
        }

        [Fact]
        public void GetDependencyGraph_WithFrameworkModules_ShouldIndicateFrameworkLevel()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();
            var frameworkModule = new TestFrameworkModule();
            var regularModule = new TestModuleA();
            
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestFrameworkModule), frameworkModule));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), regularModule));

            // Act
            var dependencyGraph = resolver.GetDependencyGraph();

            // Assert: Framework modules should be prefixed with *
            Assert.Contains("*TestFrameworkModule", dependencyGraph);
            Assert.Contains("TestModuleA", dependencyGraph);
            Assert.DoesNotContain("*TestModuleA", dependencyGraph);
        }

        [Fact]
        public void GetDependencyGraph_EmptyResolver_ShouldReturnEmptyString()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();

            // Act
            var result = resolver.GetDependencyGraph();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetDependencyGraph_WithMixedPriorities_ShouldMatchLoadOrder()
        {
            // Arrange: Framework and regular modules with no dependencies
            var resolver = new ModuleDependencyResolver();
            var frameworkA = new TestFrameworkModule();
            var regularA = new TestModuleA();
            var regularB = new TestModuleB();
            
            // Register in mixed order
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), regularA));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestFrameworkModule), frameworkA));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleB), regularB));

            // Act
            var loadOrder = resolver.ResolveLoadOrder();
            var dependencyGraph = resolver.GetDependencyGraph();

            // Assert: Graph should match load order (framework first)
            var loadOrderNames = loadOrder.Select(d => 
                d.Instance.IsFrameworkLevel ? $"*{d.ModuleType.Name}" : d.ModuleType.Name);
            var expectedGraph = string.Join(" -> ", loadOrderNames);
            Assert.Equal(expectedGraph, dependencyGraph);
            
            // Framework module should appear first
            Assert.StartsWith("*TestFrameworkModule", dependencyGraph);
        }

        [Fact]
        public void GetDependencyGraph_CalledMultipleTimes_ShouldReturnSameResult()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), new TestModuleA()));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleB), new TestModuleB()));

            // Act: Call multiple times
            var result1 = resolver.GetDependencyGraph();
            var result2 = resolver.GetDependencyGraph();
            var result3 = resolver.GetDependencyGraph();

            // Assert: All results should be identical
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        #endregion

        #region Caching Tests

        [Fact]
        public void ResolveLoadOrder_CalledMultipleTimes_ShouldUseCachedResult()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), new TestModuleA()));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleB), new TestModuleB()));

            // Act: Call multiple times
            var result1 = resolver.ResolveLoadOrder();
            var result2 = resolver.ResolveLoadOrder();

            // Assert: Should return the same instance (cached)
            Assert.Same(result1, result2);
        }

        [Fact]
        public void RegisterModule_AfterResolve_ShouldInvalidateCache()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), new TestModuleA()));
            
            // Act: Resolve, then add module, then resolve again
            var result1 = resolver.ResolveLoadOrder();
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleB), new TestModuleB()));
            var result2 = resolver.ResolveLoadOrder();

            // Assert: Results should be different (cache was invalidated)
            Assert.NotSame(result1, result2);
            Assert.Single(result1);
            Assert.Equal(2, result2.Count);
        }

        [Fact]
        public void GetDependencyGraph_AfterNewModuleRegistered_ShouldReflectNewModule()
        {
            // Arrange
            var resolver = new ModuleDependencyResolver();
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), new TestModuleA()));
            
            // Act
            var graph1 = resolver.GetDependencyGraph();
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleB), new TestModuleB()));
            var graph2 = resolver.GetDependencyGraph();

            // Assert
            Assert.Equal("TestModuleA", graph1);
            Assert.Contains("TestModuleB", graph2);
            Assert.NotEqual(graph1, graph2);
        }

        #endregion

        #region Priority Tests

        [Fact]
        public void ResolveLoadOrder_FrameworkModules_ShouldLoadBeforeRegularModules()
        {
            // Arrange: Mix of framework and regular modules with no dependencies
            var resolver = new ModuleDependencyResolver();
            var frameworkModule = new TestFrameworkModule();
            var regularA = new TestModuleA();
            var regularB = new TestModuleB();
            
            // Register in non-priority order
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), regularA));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestFrameworkModule), frameworkModule));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleB), regularB));

            // Act
            var result = resolver.ResolveLoadOrder();

            // Assert: Framework module should be first
            Assert.Equal(typeof(TestFrameworkModule), result[0].ModuleType);
            Assert.True(result[0].Instance.IsFrameworkLevel);
        }

        [Fact]
        public void ResolveLoadOrder_MiCakeRootModule_ShouldAlwaysBeFirst()
        {
            // Arrange: Include a module named MiCakeRootModule with other framework and regular modules
            var resolver = new ModuleDependencyResolver();
            var rootModule = new MiCakeRootModule();
            var frameworkModule = new TestFrameworkModule();
            var regularModule = new TestModuleA();
            
            // Register in random order
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestModuleA), regularModule));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(TestFrameworkModule), frameworkModule));
            resolver.RegisterModule(new MiCakeModuleDescriptor(typeof(MiCakeRootModule), rootModule));

            // Act
            var result = resolver.ResolveLoadOrder();

            // Assert: Root module should be first
            Assert.Equal(typeof(MiCakeRootModule), result[0].ModuleType);
            Assert.Equal("MiCakeRootModule", result[0].ModuleType.Name);
        }

        #endregion

        #region Test Helper Modules

        // Simple modules with no dependencies
        private class TestModuleA : MiCakeModule { }
        
        // B depends on A (for linear chain test)
        [RelyOn(typeof(TestModuleA))]
        private class TestModuleBDependsOnA : MiCakeModule { }
        
        // C depends on B (for linear chain test)
        [RelyOn(typeof(TestModuleBDependsOnA))]
        private class TestModuleCDependsOnB : MiCakeModule { }
        
        // B depends on A (for complex graph)
        [RelyOn(typeof(TestModuleA))]
        private class TestModuleB : MiCakeModule { }
        
        // C depends on A (for complex graph)
        [RelyOn(typeof(TestModuleA))]
        private class TestModuleC : MiCakeModule { }
        
        // D depends on B and C
        [RelyOn(typeof(TestModuleB), typeof(TestModuleC))]
        private class TestModuleD : MiCakeModule { }
        
        // E depends on C
        [RelyOn(typeof(TestModuleC))]
        private class TestModuleE : MiCakeModule { }
        
        // F depends on D and E
        [RelyOn(typeof(TestModuleD), typeof(TestModuleE))]
        private class TestModuleF : MiCakeModule { }

        // Circular dependency: A -> B -> A
        [RelyOn(typeof(TestCircularModuleB))]
        private class TestCircularModuleA : MiCakeModule { }
        
        [RelyOn(typeof(TestCircularModuleA))]
        private class TestCircularModuleB : MiCakeModule { }

        // Framework module for priority testing
        private class TestFrameworkModule : MiCakeModule 
        { 
            public override bool IsFrameworkLevel => true; 
        }

        // Test class with the exact name MiCakeRootModule for priority testing
        private class MiCakeRootModule : MiCakeModule
        {
            public override bool IsFrameworkLevel => true;
        }

        #endregion
    }
}
