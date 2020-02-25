using MiCake.Core.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.Modularity
{
    public class MiCakeModuleManager : IMiCakeModuleManager
    {
        public IMiCakeModuleCollection MiCakeModules { get; private set; } = new MiCakeModuleCollection();
        public IMiCakeModuleCollection FeatureModules { get; private set; } = new MiCakeModuleCollection();
        public IMiCakeModuleCollection AllModules { get; private set; } = new MiCakeModuleCollection();

        private bool _isPopulate;
        private List<Type> _featureModules = new List<Type>();

        internal void PopulateModules(Type startUp)
        {
            if (_isPopulate)
                throw new InvalidOperationException("PopulateDefaultModule can only be called once.");

            _isPopulate = true;

            //normal module
            foreach (var module in ResolvingMiCakeModules(startUp))
            {
                MiCakeModules.AddIfNotContains(module);
            }

            //feature module
            foreach (var featureModule in ResolvingFeatureModules(_featureModules))
            {
                FeatureModules.AddIfNotContains(featureModule);
            }

            AllModules = MiCakeModuleHelper.CombineNormalAndFeatureModules(MiCakeModules, FeatureModules);
        }

        public MiCakeModuleDescriptor GetMiCakeModule(Type moduleType)
        {
            return MiCakeModules.FirstOrDefault(s => s.Type == moduleType);
        }

        public void AddFeatureModule(Type featureModule)
        {
            MiCakeModuleHelper.CheckFeatureModule(featureModule);

            _featureModules.AddIfNotContains(featureModule);
        }

        private IEnumerable<MiCakeModuleDescriptor> ResolvingMiCakeModules(Type startUp)
        {
            List<MiCakeModuleDescriptor> miCakeModuleDescriptors = new List<MiCakeModuleDescriptor>();

            var moduleTypes = MiCakeModuleHelper.FindAllModuleTypes(startUp);
            foreach (var moduleTye in moduleTypes)
            {
                MiCakeModule instance = (MiCakeModule)Activator.CreateInstance(moduleTye);
                miCakeModuleDescriptors.Add(new MiCakeModuleDescriptor(moduleTye, instance));
            }

            return SortModulesDepencyies(miCakeModuleDescriptors);
        }

        private IEnumerable<MiCakeModuleDescriptor> ResolvingFeatureModules(List<Type> addedFeatureModules)
        {
            List<MiCakeModuleDescriptor> featureModuleDescriptors = new List<MiCakeModuleDescriptor>();

            foreach (var featureModule in addedFeatureModules)
            {
                MiCakeModuleHelper.CheckFeatureModule(featureModule);

                MiCakeModule instance = (MiCakeModule)Activator.CreateInstance(featureModule);
                featureModuleDescriptors.AddIfNotContains(new MiCakeModuleDescriptor(featureModule, instance));
            }
            return SortModulesDepencyies(featureModuleDescriptors);
        }

        //Get module dependencies
        private List<MiCakeModuleDescriptor> SortModulesDepencyies(List<MiCakeModuleDescriptor> miCakeModuleDescriptors)
        {
            foreach (var miCakeModuleDescriptor in miCakeModuleDescriptors)
            {
                var depencies = GetMiCakeModuleDescriptorDepencyies(miCakeModuleDescriptors, miCakeModuleDescriptor);

                foreach (var depency in depencies)
                {
                    miCakeModuleDescriptor.AddDependency(depency);
                }
            }
            // sort by modules dependencies
            miCakeModuleDescriptors = miCakeModuleDescriptors.SortByDependencies(s => s.Dependencies);

            return miCakeModuleDescriptors;
        }

        private List<MiCakeModuleDescriptor> GetMiCakeModuleDescriptorDepencyies(
            List<MiCakeModuleDescriptor> modules,
            MiCakeModuleDescriptor moduleDescriptor)
        {
            List<MiCakeModuleDescriptor> descriptors = new List<MiCakeModuleDescriptor>();

            var depencyTypes = MiCakeModuleHelper.FindDependedModuleTypes(moduleDescriptor.Type);

            foreach (var depencyType in depencyTypes)
            {
                var existDescriptor = modules.FirstOrDefault(s => s.Type == depencyType);
                if (existDescriptor != null) moduleDescriptor.AddDependency(existDescriptor);
            }

            return descriptors;
        }
    }
}
