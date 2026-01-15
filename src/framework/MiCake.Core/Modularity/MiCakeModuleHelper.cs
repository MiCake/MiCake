using MiCake.Util.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Helper class for MiCake module operations.
    /// Provides utilities for module validation, dependency resolution, and discovery.
    /// </summary>
    internal static class MiCakeModuleHelper
    {
        /// <summary>
        /// Checks if the given type is a valid MiCake module.
        /// A valid module must be a non-abstract, non-generic class that inherits from MiCakeModule.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is a valid MiCake module, false otherwise</returns>
        internal static bool IsMiCakeModule(Type type)
        {
            if (type == null)
                return false;

            var typeInfo = type.GetTypeInfo();

            return typeInfo.IsClass &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.IsGenericType &&
                   typeof(MiCakeModule).GetTypeInfo().IsAssignableFrom(type);
        }

        /// <summary>
        /// Validates that the given type is a MiCake module.
        /// Throws an exception if the type is not valid.
        /// </summary>
        /// <param name="type">The type to validate</param>
        /// <exception cref="ArgumentNullException">When type is null</exception>
        /// <exception cref="ArgumentException">When type is not a valid MiCake module</exception>
        internal static void CheckModule(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "Module type cannot be null.");

            if (!IsMiCakeModule(type))
            {
                throw new ArgumentException(
                    $"The given type is not a valid MiCake module: {type.AssemblyQualifiedName}. " +
                    $"Modules must be non-abstract, non-generic classes that inherit from MiCakeModule.",
                    nameof(type));
            }
        }

        /// <summary>
        /// Finds all dependent module types for the specified module using RelyOnAttribute.
        /// </summary>
        /// <param name="moduleType">The module type to find dependencies for</param>
        /// <returns>A list of dependent module types</returns>
        internal static List<Type> FindDependedModuleTypes(Type moduleType)
        {
            CheckModule(moduleType);

            var dependencies = new List<Type>();

            var dependencyDescriptors = moduleType
                .GetCustomAttributes()
                .OfType<RelyOnAttribute>();

            foreach (var descriptor in dependencyDescriptors)
            {
                foreach (var dependedModuleType in descriptor.GetRelyOnTypes())
                {
                    dependencies.AddIfNotContains(dependedModuleType);
                }
            }

            return dependencies;
        }

        /// <summary>
        /// Recursively finds all modules starting from an entry module.
        /// This method traverses the dependency tree and collects all required modules.
        /// </summary>
        /// <param name="moduleTypes">The collection to add discovered modules to</param>
        /// <param name="entryModule">The entry point module to start discovery from</param>
        /// <exception cref="ArgumentNullException">When moduleTypes or entryModule is null</exception>
        internal static void FindAllModulesFromEntry(ICollection<Type> moduleTypes, Type entryModule)
        {
            ArgumentNullException.ThrowIfNull(moduleTypes);

            CheckModule(entryModule);

            // Avoid processing the same module twice
            if (moduleTypes.Contains(entryModule))
            {
                return;
            }

            moduleTypes.Add(entryModule);

            // Recursively process dependencies
            foreach (var dependedModuleType in FindDependedModuleTypes(entryModule))
            {
                FindAllModulesFromEntry(moduleTypes, dependedModuleType);
            }
        }
    }
}
