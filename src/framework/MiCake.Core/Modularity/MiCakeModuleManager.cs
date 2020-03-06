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

        private bool _isPopulate;
        public bool IsPopulated => _isPopulate;

        private List<Type> _featureModulesType = new List<Type>();
        private List<Type> _externalModulesType = new List<Type>();

        public void PopulateModules(Type entryType)
        {
            if (_isPopulate)
                throw new InvalidOperationException("PopulateDefaultModule can only be called once.");

            _isPopulate = true;

            var normalModulesType = GetNormalModuleTypes(entryType);

            IMiCakeModuleCollection normalModules = ResolvingMiCakeModules(normalModulesType)
                                                            .ToMiCakeModuleCollection();

            IMiCakeModuleCollection featureModules = ResolvingMiCakeModules(_featureModulesType)
                                                            .ToMiCakeModuleCollection();

            IMiCakeModuleCollection allModules = MiCakeModuleHelper.CombineNormalAndFeatureModules(normalModules, featureModules);

            _moduleContext = new MiCakeModuleContext(normalModules, featureModules, allModules);
        }

        public MiCakeModuleDescriptor GetMiCakeModule(Type moduleType)
        {
            return _moduleContext.AllModules.FirstOrDefault(s => s.Type == moduleType);
        }

        private List<Type> GetNormalModuleTypes(Type entryType)
        {
            var internalTypes = MiCakeModuleHelper.FindAllModuleTypes(entryType);
            return internalTypes.Union(_externalModulesType).ToList();
        }

        public void AddFeatureModule(Type featureModule)
        {
            MiCakeModuleHelper.CheckFeatureModule(featureModule);
            _featureModulesType.AddIfNotContains(featureModule);
        }

        public void AddMiCakeModule(Type moduleType)
        {
            MiCakeModuleHelper.CheckModule(moduleType);

            //add denpend on  modules
            var externalDependedModules = MiCakeModuleHelper.FindDependedModuleTypes(moduleType);
            _externalModulesType.AddIfNotContains(externalDependedModules);

            _externalModulesType.AddIfNotContains(moduleType);
        }

        // Get the description information (including dependency and order) of the module according to its type
        private IEnumerable<MiCakeModuleDescriptor> ResolvingMiCakeModules(List<Type> moduleTypes)
        {
            List<MiCakeModuleDescriptor> miCakeModuleDescriptors = new List<MiCakeModuleDescriptor>();

            foreach (var moduleTye in moduleTypes)
            {
                MiCakeModule instance = (MiCakeModule)ServiceCtor(moduleTye);
                miCakeModuleDescriptors.Add(new MiCakeModuleDescriptor(moduleTye, instance));
            }

            return SortModulesDepencyies(miCakeModuleDescriptors);
        }

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

        //Get module dependencies
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
