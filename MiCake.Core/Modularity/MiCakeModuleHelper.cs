using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.Core.Modularity
{
    internal static class MiCakeModuleHelper
    {
        internal static bool IsMiCakeModule(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            return
                typeInfo.IsClass &&
                !typeInfo.IsAbstract &&
                !typeInfo.IsGenericType &&
                typeof(IMiCakeModule).GetTypeInfo().IsAssignableFrom(type);
        }

        internal static void CheckModule(Type type)
        {
            if (!IsMiCakeModule(type))
            {
                throw new ArgumentException("Given type is not an MiCake module: " + type.AssemblyQualifiedName);
            }
        }

        internal static List<Type> FindAllModuleTypes(Type startupModuleType)
        {
            var moduleTypes = new List<Type>();
            AddModuleAndDependenciesResursively(moduleTypes, startupModuleType);
            return moduleTypes;
        }

        internal static List<Type> FindDependedModuleTypes(Type moduleType)
        {
            CheckModule(moduleType);

            var dependencies = new List<Type>();

            var dependencyDescriptors = moduleType
                .GetCustomAttributes()
                .OfType<DependOnAttribute>();

            foreach (var descriptor in dependencyDescriptors)
            {
                foreach (var dependedModuleType in descriptor.GetDependedTypes())
                {
                    dependencies.AddIfNotContains(dependedModuleType);
                }
            }

            return dependencies;
        }

        internal static void AddModuleAndDependenciesResursively(List<Type> moduleTypes, Type moduleType)
        {
            CheckModule(moduleType);

            if (moduleTypes.Contains(moduleType))
            {
                return;
            }

            moduleTypes.Add(moduleType);

            foreach (var dependedModuleType in FindDependedModuleTypes(moduleType))
            {
                AddModuleAndDependenciesResursively(moduleTypes, dependedModuleType);
            }
        }
    }
}
