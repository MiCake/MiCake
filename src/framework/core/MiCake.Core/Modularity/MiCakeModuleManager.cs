using MiCake.Core.Util.Collections;

namespace MiCake.Core.Modularity
{
    public class MiCakeModuleManager : IMiCakeModuleManager, IModuleSlot
    {
        public Func<Type, object?> ServiceCtor { get; private set; } = Activator.CreateInstance;

        private MiCakeModuleContext _moduleContext = new();
        public IMiCakeModuleContext ModuleContext => _moduleContext;

        private bool _isPopulate;
        public bool IsPopulated => _isPopulate;

        private readonly List<Type> modulesTypes = new();

        public void PopulateModules(Type entryType, IMiCakeModuleSorter? customSorter = null)
        {
            if (_isPopulate)
                throw new InvalidOperationException("PopulateDefaultModule can only be called once.");

            _isPopulate = true;

            MiCakeModuleHelper.FindAllModulesFromEntry(modulesTypes, entryType);
            IMiCakeModuleCollection allModules = ResolvingMiCakeModules(modulesTypes).ToMiCakeModuleCollection();

            //Ensure that the position of the entry module is the last
            if (allModules[^1].ModuleType != entryType)
            {
                allModules.ExchangeOrder(s => s.ModuleType == entryType, allModules.Count - 1);
            }

            if (customSorter != null)
            {
                allModules = customSorter.Sort(allModules);
            }
            _moduleContext = new MiCakeModuleContext(allModules);
        }

        public MiCakeModuleDescriptor? GetMiCakeModule(Type moduleType)
        {
            return _moduleContext.MiCakeModules.FirstOrDefault(s => s.ModuleType == moduleType);
        }

        // Get the description information (including dependency and order) of the module according to its type
        private IEnumerable<MiCakeModuleDescriptor> ResolvingMiCakeModules(List<Type> moduleTypes)
        {
            List<MiCakeModuleDescriptor> miCakeModuleDescriptors = new();

            foreach (var moduleTye in moduleTypes)
            {
                MiCakeModule instance = (MiCakeModule)(ServiceCtor(moduleTye) ?? throw new NullReferenceException($"can not create instance of module, type is {moduleTye.FullName}"));
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
        private List<MiCakeModuleDescriptor> GetMiCakeModuleDescriptorDepencyies(List<MiCakeModuleDescriptor> modules, MiCakeModuleDescriptor moduleDescriptor)
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

        public void Slot(Type moduleType)
        {
            MiCakeModuleHelper.CheckModule(moduleType);

            //add denpend on modules
            MiCakeModuleHelper.FindAllModulesFromEntry(modulesTypes, moduleType);
        }

        public void Slot<TModule>() where TModule : IMiCakeModule
        {
            Slot(typeof(TModule));
        }
    }
}
