using System;

namespace MiCake.Core.Modularity
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DependOnAttribute : Attribute, IDependedTypesProvider
    {
        public Type[] DependedTypes { get; }

        public DependOnAttribute(params Type[] dependedTypes)
        {
            DependedTypes = dependedTypes ?? new Type[0];
        }

        public virtual Type[] GetDependedTypes()
        {
            return DependedTypes;
        }
    }
}
