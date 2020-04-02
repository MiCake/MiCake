using JetBrains.Annotations;
using MiCake.DDD.Domain.Store;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MiCake.DDD.Domain.Helper
{
    public static class EntityHelper
    {
        public static bool IsEntity([NotNull] Type type)
        {
            return typeof(IEntity).IsAssignableFrom(type);
        }

        public static bool IsAggregateRoot([NotNull] Type type)
        {
            return typeof(IAggregateRoot).IsAssignableFrom(type);
        }

        public static bool HasPersistentObject([NotNull] Type type)
        {
            return typeof(IHasPersistentObject).IsAssignableFrom(type);
        }

        public static bool HasDefaultId<TKey>([NotNull] IEntity<TKey> entity)
        {
            if (EqualityComparer<TKey>.Default.Equals(entity.Id, default))
            {
                return true;
            }

            return false;
        }

        public static Type FindEntityPersistentType<TEntity>()
            where TEntity : IEntity, IHasPersistentObject
        {
            return FindEntityPersistentType(typeof(TEntity));
        }

        public static Type FindEntityPersistentType(Type entityType)
        {
            if (!typeof(IEntity).IsAssignableFrom(entityType))
            {
                throw new ArgumentException($"Given {nameof(entityType)} is not an entity. It should implement {typeof(IEntity).AssemblyQualifiedName}!");
            }

            if (!typeof(IHasPersistentObject).IsAssignableFrom(entityType))
            {
                throw new ArgumentException($"Given {nameof(entityType)} is not an entity. It should implement {typeof(IHasPersistentObject).AssemblyQualifiedName}!");
            }

            return null;
        }

        public static Type FindPrimaryKeyType<TEntity>()
            where TEntity : IEntity
        {
            return FindPrimaryKeyType(typeof(TEntity));
        }

        public static Type FindPrimaryKeyType([NotNull] Type entityType)
        {
            if (!typeof(IEntity).IsAssignableFrom(entityType))
            {
                throw new ArgumentException($"Given {nameof(entityType)} is not an entity. It should implement {typeof(IEntity).AssemblyQualifiedName}!");
            }

            foreach (var interfaceType in entityType.GetTypeInfo().GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEntity<>))
                {
                    return interfaceType.GenericTypeArguments[0];
                }
            }

            return null;
        }

    }
}
