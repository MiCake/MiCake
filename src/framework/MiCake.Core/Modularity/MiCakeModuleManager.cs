using MiCake.Core.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.Modularity
{
    public class MiCakeModuleManager : IMiCakeModuleManager
    {
        public Func<Type, object> ServiceCtor { get; private set; } = Activator.CreateInstance;

        private MiCakeModuleContext _moduleContext;
        public IMiCakeModuleContext ModuleContext => _moduleContext;

        public bool IsPopulated => throw new NotImplementedException();

        private bool _isPopulate;
        private List<Type> _featureModules = new List<Type>();

        public void PopulateModules(Type entryType)
        {
            if (_isPopulate)
                throw new InvalidOperationException("PopulateDefaultModule can only be called once.");

            _isPopulate = true;

            IMiCakeModuleCollection normalModules = new MiCakeModuleCollection();
            IMiCakeModuleCollection featureModules = new MiCakeModuleCollection();
            IMiCakeModuleCollection allModules = new MiCakeModuleCollection();

            //normal modules
            foreach (var module in ResolvingMiCakeModules(entryType))
            {
                normalModules.AddIfNotContains(module);
            }

            //feature modules
            foreach (var featureModule in ResolvingFeatureModules(_featureModules))
            {
                featureModules.AddIfNotContains(featureModule);
            }

            //all modules
            allModules = MiCakeModuleHelper.CombineNormalAndFeatureModules(normalModules, featureModules);

            _moduleContext = new MiCakeModuleContext(normalModules, featureModules, allModules);
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
                MiCakeModule instance = (MiCakeModule)ServiceCtor(moduleTye);
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

                MiCakeModule instance = (MiCakeModule)ServiceCtor(featureModule);
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

        public void AddMiCakeModule()
        {
            throw new NotImplementedException();
        }

        void IMiCakeModuleManager.PopulateModules(Type entryType)
        {
            throw new NotImplementedException();
        }
    }
}
