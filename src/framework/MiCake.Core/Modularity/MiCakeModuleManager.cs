using MiCake.Core.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    public class MiCakeModuleManager : IMiCakeModuleManager
    {
        public Func<Type, object> ServiceCtor { get; private set; } = Activator.CreateInstance;

        private MiCakeModuleContext _moduleContext;
        public IMiCakeModuleContext ModuleContext => _moduleContext;

        private bool _isPopulate;
        public bool IsPopulated => _isPopulate;

        private readonly List<Type> _normalModulesTypes = [];

        public Task PopulateModules(Type entryType)
        {
            if (_isPopulate)
                throw new InvalidOperationException("PopulateDefaultModule can only be called once.");

            _isPopulate = true;

            MiCakeModuleHelper.FindAllModulesFromEntry(_normalModulesTypes, entryType);
            IMiCakeModuleCollection modules = ResolvingMiCakeModules(_normalModulesTypes).ToMiCakeModuleCollection();

            //Ensure that the position of the entry module is the last
            if (modules[^1].ModuleType != entryType)
            {
                modules.ExchangeOrder(s => s.ModuleType == entryType, modules.Count - 1);
            }

            _moduleContext = new MiCakeModuleContext(modules);

            return Task.CompletedTask;
        }

        public MiCakeModuleDescriptor GetMiCakeModule(Type moduleType)
        {
            return _moduleContext.MiCakeModules.FirstOrDefault(s => s.ModuleType == moduleType);
        }

        public Task AddMiCakeModule(Type moduleType)
        {
            MiCakeModuleHelper.CheckModule(moduleType);

            //add denpend on modules
            MiCakeModuleHelper.FindAllModulesFromEntry(_normalModulesTypes, moduleType);

            return Task.CompletedTask;
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
        private static List<MiCakeModuleDescriptor> GetMiCakeModuleDescriptorDepencyies(
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
