using System;
using System.Linq;
using System.Reflection;

namespace MiCake.DDD.Domain.Helper
{
    public static class EntityFinder
    {
        public static Type[] FindAllEntity(Assembly assembly)
        {
            return assembly.GetTypes().Where(type => type is IEntity).ToArray();
        }

        public static Type[] FindAllAggregateRoot(Assembly assembly)
        {
            return assembly.GetTypes().Where(type => type is IAggregateRoot).ToArray();
        }
    }
}
