using JetBrains.Annotations;
using MiCake.Core.Util.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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

        public static bool IsEntityHasSnapshot([NotNull] Type type)
        {
            return typeof(IEntityHasSnapshot).IsAssignableFrom(type);
        }

        public static bool HasDefaultId<TKey>([NotNull] IEntity<TKey> entity)
        {
            if (EqualityComparer<TKey>.Default.Equals(entity.Id, default))
            {
                return true;
            }

            return false;
        }

        public static bool IsSnapshotEntity([NotNull]Type type)
        {
            return typeof(IEntityHasSnapshot).IsAssignableFrom(type);
        }

        public static Type FindEntitySnapshotType<TEntity>()
            where TEntity : IEntity, IEntityHasSnapshot
        {
            return FindEntitySnapshotType(typeof(TEntity));
        }

        public static Type FindEntitySnapshotType(Type entityType)
        {
            if (!typeof(IEntity).IsAssignableFrom(entityType))
            {
                throw new ArgumentException($"Given {nameof(entityType)} is not an entity. It should implement {typeof(IEntity).AssemblyQualifiedName}!");
            }

            if (!typeof(IEntityHasSnapshot).IsAssignableFrom(entityType))
            {
                throw new ArgumentException($"Given {nameof(entityType)} is not an entity. It should implement {typeof(IEntityHasSnapshot).AssemblyQualifiedName}!");
            }

            return TypeHelper.GetGenericArguments(entityType, typeof(IEntityHasSnapshot<>))[0];
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
