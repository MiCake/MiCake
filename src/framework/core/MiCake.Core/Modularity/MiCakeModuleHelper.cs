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

            Queue<IMiCakeModuleCollection> moduleQueue = new();
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
                bool depencyIsAfter = featureModule.RelyOnModules.Any(module =>
                                        ((IFeatureModule)module.Instance).Order == FeatureModuleLoadOrder.AfterCommonModule);

                var currentOrder = ((IFeatureModule)featureModule.Instance).Order;
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

        /// <summary>
        /// Find all dependent modules according to <see cref="RelyOnAttribute"/>
        /// </summary>
        /// <param name="moduleType"></param>
        /// <returns></returns>
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
        /// Find all dependent modules entering from the entry point
        /// </summary>
        /// <param name="moduleTypes">A collection of modules</param>
        /// <param name="entryModule">Module as search entry point</param>
        internal static void FindAllModulesFromEntry(ICollection<Type> moduleTypes, Type entryModule)
        {
            CheckModule(entryModule);

            if (moduleTypes.Contains(entryModule))
            {
                return;
            }

            moduleTypes.Add(entryModule);

            foreach (var dependedModuleType in FindDependedModuleTypes(entryModule))
            {
                FindAllModulesFromEntry(moduleTypes, dependedModuleType);
            }
        }
    }
}
