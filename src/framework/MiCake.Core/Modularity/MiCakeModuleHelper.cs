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
                typeof(MiCakeModule).GetTypeInfo().IsAssignableFrom(type);
        }

        internal static bool IsFeatureModule(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            return
                typeInfo.IsClass &&
                !typeInfo.IsAbstract &&
                !typeInfo.IsGenericType &&
                typeof(IFeatureModule).GetTypeInfo().IsAssignableFrom(type);
        }

        internal static void CheckModule(Type type)
        {
            if (!IsMiCakeModule(type))
            {
                throw new ArgumentException("Given type is not an MiCake module: " + type.AssemblyQualifiedName);
            }
        }

        internal static void CheckFeatureModule(Type type)
        {
            if (!IsFeatureModule(type))
            {
                throw new ArgumentException("Given type is not an Feature module: " + type.AssemblyQualifiedName);
            }

            if (!IsMiCakeModule(type))
            {
                throw new ArgumentException("Given type is not an MiCake module: " + type.AssemblyQualifiedName);
            }
        }

        internal static IMiCakeModuleCollection CombineNormalAndFeatureModules(
            IMiCakeModuleCollection normalModules,
            IMiCakeModuleCollection featureModules)
        {
            IMiCakeModuleCollection miCakeModules = new MiCakeModuleCollection();
            var assignFeatureModules = AssignFeatureModules(featureModules);

            Queue<IMiCakeModuleCollection> moduleQueue = new Queue<IMiCakeModuleCollection>();
            moduleQueue.Enqueue(assignFeatureModules.before);
            moduleQueue.Enqueue(normalModules);
            moduleQueue.Enqueue(assignFeatureModules.after);

            var count = moduleQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var modules = moduleQueue.Dequeue();
                foreach (var module in modules)
                {
                    miCakeModules.AddIfNotContains(module);
                }
            }
            return miCakeModules;
        }

        private static (IMiCakeModuleCollection before, IMiCakeModuleCollection after) AssignFeatureModules(IMiCakeModuleCollection featureModules)
        {
            var beforeFeatureModules = new MiCakeModuleCollection();
            var afterFeatureModules = new MiCakeModuleCollection();

            foreach (var featureModule in featureModules)
            {
                bool depencyIsAfter = featureModule.Dependencies.Any(module =>
                                        ((IFeatureModule)module.ModuleInstance).Order == FeatureModuleLoadOrder.AfterCommonModule);

                var currentOrder = ((IFeatureModule)featureModule.ModuleInstance).Order;
                if (!depencyIsAfter && currentOrder == FeatureModuleLoadOrder.BeforeCommonModule)
                {
                    beforeFeatureModules.Add(featureModule);
                }
                else
                {
                    afterFeatureModules.Add(featureModule);
                }
            }

            return (beforeFeatureModules, afterFeatureModules);
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
