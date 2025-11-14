using MiCake.Util.Collection;
using MiCake.Util.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Manages MiCake modules - handles module discovery, dependency resolution, and lifecycle.
    /// </summary>
    public class MiCakeModuleManager : IMiCakeModuleManager
    {
        private MiCakeModuleContext _moduleContext;
        private bool _isPopulated;
        private readonly List<Type> _normalModulesTypes = [];

        /// <summary>
        /// Function used to create module instances.
        /// Uses compiled expression trees for better performance.
        /// </summary>
        public Func<Type, object> ServiceCtor { get; private set; } = CompiledActivator.CreateInstance;

        /// <summary>
        /// Gets the module context containing all registered modules.
        /// </summary>
        public IMiCakeModuleContext ModuleContext => _moduleContext;

        /// <summary>
        /// Indicates whether modules have been populated.
        /// </summary>
        public bool IsPopulated => _isPopulated;

        /// <summary>
        /// Discovers and registers all modules starting from the entry module.
        /// This method performs dependency resolution and topological sorting.
        /// </summary>
        /// <param name="entryType">The entry module type</param>
        /// <exception cref="InvalidOperationException">When called more than once</exception>
        /// <exception cref="ArgumentNullException">When entryType is null</exception>
        public void PopulateModules(Type entryType)
        {
            ArgumentNullException.ThrowIfNull(entryType);

            if (_isPopulated)
                throw new InvalidOperationException("PopulateModules can only be called once. Modules have already been populated.");

            _isPopulated = true;

            // Find all modules recursively from entry module
            MiCakeModuleHelper.FindAllModulesFromEntry(_normalModulesTypes, entryType);

            // Resolve and sort modules by dependencies
            IMiCakeModuleCollection modules = ResolvingMiCakeModules(_normalModulesTypes).ToMiCakeModuleCollection();

            // Ensure that the entry module is the last in the collection
            // This guarantees it runs after all its dependencies
            if (modules[^1].ModuleType != entryType)
            {
                modules.ExchangeOrder(s => s.ModuleType == entryType, modules.Count - 1);
            }

            _moduleContext = new MiCakeModuleContext(modules);
        }

        /// <summary>
        /// Gets a specific module descriptor by type.
        /// </summary>
        /// <param name="moduleType">The module type to find</param>
        /// <returns>The module descriptor, or null if not found</returns>
        public MiCakeModuleDescriptor GetMiCakeModule(Type moduleType)
        {
            ArgumentNullException.ThrowIfNull(moduleType);

            return _moduleContext?.MiCakeModules.FirstOrDefault(s => s.ModuleType == moduleType);
        }

        /// <summary>
        /// Adds a module and its dependencies to the module collection.
        /// This method is typically used to add modules dynamically.
        /// </summary>
        /// <param name="moduleType">The module type to add</param>
        public void AddMiCakeModule(Type moduleType)
        {
            MiCakeModuleHelper.CheckModule(moduleType);

            // Add the module and all its dependencies
            MiCakeModuleHelper.FindAllModulesFromEntry(_normalModulesTypes, moduleType);
        }

        /// <summary>
        /// Creates module descriptors from module types and resolves their dependencies.
        /// Uses ModuleDependencyResolver with Kahn's algorithm for topological sorting.
        /// </summary>
        /// <param name="moduleTypes">The list of module types to process</param>
        /// <returns>A sorted list of module descriptors in dependency order</returns>
        private List<MiCakeModuleDescriptor> ResolvingMiCakeModules(List<Type> moduleTypes)
        {
            var resolver = new ModuleDependencyResolver();

            // Create instances and register all modules
            foreach (var moduleType in moduleTypes)
            {
                MiCakeModule instance = (MiCakeModule)ServiceCtor(moduleType);
                var descriptor = new MiCakeModuleDescriptor(moduleType, instance);
                resolver.RegisterModule(descriptor);
            }

            // Resolve and return sorted modules
            return resolver.ResolveLoadOrder();
        }
    }
}
