using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using System;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Describes an <see cref="IEntity"/>
    /// </summary>
    public class EntityDescriptor : DomainObjectDescriptor
    {
        /// <summary>
        /// The primary key of <see cref="IEntity"/>
        /// </summary>
        public Type PrimaryKey { get; }

        public EntityDescriptor(Type type) : base(type)
        {
            PrimaryKey = EntityHelper.FindPrimaryKeyType(type);
        }
    }
}
