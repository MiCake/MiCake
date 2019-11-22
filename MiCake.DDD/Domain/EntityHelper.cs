using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain
{
   public static class EntityHelper
    {
        public static bool IsEntity([NotNull] Type type)
        {
            return typeof(IEntity).IsAssignableFrom(type);
        }

        public static bool HasDefaultId<TKey>([NotNull] IEntity<TKey> entity)
        {
            if (EqualityComparer<TKey>.Default.Equals(entity.Id, default))
            {
                return true;
            }

            return false;
        }
    }
}
