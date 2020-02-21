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

        public static bool HasStorageModel([NotNull] Type type)
        {
            return typeof(IHasStorageModel).IsAssignableFrom(type);
        }

        public static bool HasDefaultId<TKey>([NotNull] IEntity<TKey> entity)
        {
            if (EqualityComparer<TKey>.Default.Equals(entity.Id, default))
            {
                return true;
            }

            return false;
        }

        public static Type FindEntityStorageType<TEntity>()
            where TEntity : IEntity, IHasStorageModel
        {
            return FindEntityStorageType(typeof(TEntity));
        }

        public static Type FindEntityStorageType(Type entityType)
        {
            if (!typeof(IEntity).IsAssignableFrom(entityType))
            {
                throw new ArgumentException($"Given {nameof(entityType)} is not an entity. It should implement {typeof(IEntity).AssemblyQualifiedName}!");
            }

            if (!typeof(IHasStorageModel).IsAssignableFrom(entityType))
            {
                throw new ArgumentException($"Given {nameof(entityType)} is not an entity. It should implement {typeof(IHasStorageModel).AssemblyQualifiedName}!");
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
