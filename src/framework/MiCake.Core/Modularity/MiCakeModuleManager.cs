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

        private readonly List<Type> _featureModulesTypes = new();
        private readonly List<Type> _normalModulesTypes = new();

        public void PopulateModules(Type entryType)
        {
            if (_isPopulate)
                throw new InvalidOperationException("PopulateDefaultModule can only be called once.");

            _isPopulate = true;

            MiCakeModuleHelper.FindAllModulesFromEntry(_normalModulesTypes, entryType);
            IMiCakeModuleCollection normalModules = ResolvingMiCakeModules(_normalModulesTypes)
                                                            .ToMiCakeModuleCollection();
            //Ensure that the position of the entry module is the last
            if (normalModules[^1].ModuleType != entryType)
            {
                normalModules.ExchangeOrder(s => s.ModuleType == entryType, normalModules.Count - 1);
            }

            IMiCakeModuleCollection featureModules = ResolvingMiCakeModules(_featureModulesTypes)
                                                            .ToMiCakeModuleCollection();

            IMiCakeModuleCollection allModules = MiCakeModuleHelper.CombineNormalAndFeatureModules(normalModules, featureModules);

            _moduleContext = new MiCakeModuleContext(allModules, normalModules, featureModules);
        }

        public MiCakeModuleDescriptor GetMiCakeModule(Type moduleType)
        {
            return _moduleContext.AllModules.FirstOrDefault(s => s.ModuleType == moduleType);
        }

        public void AddFeatureModule(Type featureModule)
        {
            MiCakeModuleHelper.CheckFeatureModule(featureModule);
            _featureModulesTypes.AddIfNotContains(featureModule);
        }

        public void AddMiCakeModule(Type moduleType)
        {
            MiCakeModuleHelper.CheckModule(moduleType);

            //add denpend on  modules
            MiCakeModuleHelper.FindAllModulesFromEntry(_normalModulesTypes, moduleType);
        }

        // Get the description information (including dependency and order) of the module according to its type
        private IEnumerable<MiCakeModuleDescriptor> ResolvingMiCakeModules(List<Type> moduleTypes)
        {
            List<MiCakeModuleDescriptor> miCakeModuleDescriptors = new();

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
            miCakeModuleDescriptors = miCakeModuleDescriptors.SortByDependencies(s => s.RelyOnModules);

            return miCakeModuleDescriptors;
        }

        //Get module dependencies
        private List<MiCakeModuleDescriptor> GetMiCakeModuleDescriptorDepencyies(
            List<MiCakeModuleDescriptor> modules,
            MiCakeModuleDescriptor moduleDescriptor)
        {
            List<MiCakeModuleDescriptor> descriptors = new();

            var depencyTypes = MiCakeModuleHelper.FindDependedModuleTypes(moduleDescriptor.ModuleType);

            foreach (var depencyType in depencyTypes)
            {
                var existDescriptor = modules.FirstOrDefault(s => s.ModuleType == depencyType);
                if (existDescriptor != null) moduleDescriptor.AddDependency(existDescriptor);
            }

            return descriptors;
        }
    }
}
