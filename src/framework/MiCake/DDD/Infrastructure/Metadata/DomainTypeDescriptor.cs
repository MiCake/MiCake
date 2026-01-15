using System;

namespace MiCake.DDD.Infrastructure.Metadata
{
    /// <summary>
    /// Base descriptor for all domain objects
    /// </summary>
    public abstract class DomainTypeDescriptor(Type type)
    {
        /// <summary>
        /// The CLR type of the domain object
        /// </summary>
        public Type Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

        /// <summary>
        /// The name of the domain object
        /// </summary>
        public string Name => Type.Name;
    }

    /// <summary>
    /// Describes an Entity with a primary key
    /// </summary>
    public class EntityDescriptor : DomainTypeDescriptor
    {
        /// <summary>
        /// The primary key type of the entity
        /// </summary>
        public Type KeyType { get; }

        public EntityDescriptor(Type entityType, Type keyType) : base(entityType)
        {
            KeyType = keyType ?? throw new ArgumentNullException(nameof(keyType));
        }
    }

    /// <summary>
    /// Describes an Aggregate Root
    /// </summary>
    public class AggregateRootDescriptor : EntityDescriptor
    {
        public AggregateRootDescriptor(Type aggregateType, Type keyType) 
            : base(aggregateType, keyType)
        {
        }
    }

    /// <summary>
    /// Describes a Value Object
    /// </summary>
    public class ValueObjectDescriptor : DomainTypeDescriptor
    {
        public ValueObjectDescriptor(Type valueObjectType) : base(valueObjectType)
        {
        }
    }
}
