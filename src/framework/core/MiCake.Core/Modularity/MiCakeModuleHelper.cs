using MiCake.Core.Util.Collections;
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

        internal static void CheckModule(Type type)
        {
            if (!IsMiCakeModule(type))
            {
                throw new ArgumentException("Given type is not an MiCake module: " + type.AssemblyQualifiedName);
            }
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
