using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using System;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Describes an <see cref="IAggregateRoot"/>
    /// </summary>
    public class AggregateRootDescriptor : DomainObjectDescriptor
    {
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
        }
    }
}
