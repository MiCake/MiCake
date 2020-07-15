using MiCake.Core.Util.Reflection;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;
using System;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Describes an <see cref="IAggregateRoot"/>
    /// </summary>
    public class AggregateRootDescriptor : DomainObjectDescriptor
    {
        /// <summary>
        /// Is the aggregate root declared as <see cref="IHasPersistentObject"/>
        /// </summary>
        public bool HasPersistentObject { get; private set; }

        /// <summary>
        /// The persistent object corresponding to the aggregate root
        /// </summary>
        public Type PersistentObject { get; private set; }

        private Type _keyType;
        /// <summary>
        /// The primary key of <see cref="IEntity"/>
        /// </summary>
        public Type PrimaryKey
        {
            get
            {
                if (_keyType == null)
                {
                    _keyType = EntityHelper.FindPrimaryKeyType(Type);
                }
                return _keyType;
            }
        }

        public AggregateRootDescriptor(Type type) : base(type)
        {
            HasPersistentObject = EntityHelper.HasPersistentObject(type);
        }

        public void SetPersistentObject(Type persistentType)
        {
            if (!ReflectionHelper.IsAssignableToGenericType(persistentType, typeof(IPersistentObject<,>)))
                throw new ArgumentException($"The type {persistentType.Name} is not implements/inherits {nameof(PersistentObject)}.");

            var entityType = TypeHelper.GetGenericArguments(persistentType, typeof(IPersistentObject<,>))?[1];
            if (!Type.Equals(entityType))
                throw new ArgumentException($"The type {persistentType.Name} generic parameter must be {Type.Name}.But now is {entityType?.Name}");

            PersistentObject = persistentType;
        }
    }
}
