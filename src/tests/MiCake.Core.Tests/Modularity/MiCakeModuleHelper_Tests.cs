using MiCake.Core.Modularity;
using MiCake.Core.Tests.Modularity.Fakes;
using System;
using Xunit;

namespace MiCake.Core.Tests.Modularity
{
    public class MiCakeModuleHelper_Tests
    {
        [Fact]
        public void Combine_ShouldHasRightOrder()
        {
            // normal:  B->A
            var normalA = new MiCakeModuleDescriptor(typeof(DepencyModuleA), (MiCakeModule)Activator.CreateInstance(typeof(DepencyModuleA)));
            var normalB = new MiCakeModuleDescriptor(typeof(DepencyModuleB), (MiCakeModule)Activator.CreateInstance(typeof(DepencyModuleB)));
            normalB.AddDependency(normalA);

            IMiCakeModuleCollection normalModules = new MiCakeModuleCollection()
            {
               normalA,
               normalB
            };

            // feature: B(before) -> A（after） 
            var featureA = new MiCakeModuleDescriptor(typeof(FeatureModuleA), (MiCakeModule)Activator.CreateInstance(typeof(FeatureModuleA)));
            var featureB = new MiCakeModuleDescriptor(typeof(FeatureModuleBDepencyModuleA), (MiCakeModule)Activator.CreateInstance(typeof(FeatureModuleBDepencyModuleA)));
            featureB.AddDependency(featureA);

            IMiCakeModuleCollection featureModules = new MiCakeModuleCollection()
            {
                featureA,
                featureB
            };

            // expected: normalA <- normalB <- featureA <- featureB
            // Although the order of B shows the flag to start at the beginning, the A it depends on starts later, so it must start after A
            var modules = MiCakeModuleHelper.CombineNormalAndFeatureModules(normalModules, featureModules);

            var first = modules[0];
            Assert.Equal(typeof(DepencyModuleA), first.ModuleType);

            var last = modules[^1];
            Assert.Equal(typeof(FeatureModuleBDepencyModuleA), last.ModuleType);
        }
    }
}
