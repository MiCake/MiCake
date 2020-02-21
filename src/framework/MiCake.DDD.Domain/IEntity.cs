using System.Collections.Generic;

namespace MiCake.DDD.Domain
{
    public interface IEntity
    {
        /// <summary>
        /// Collection of domain events
        /// </summary>
        List<IDomainEvent> DomainEvents { get; }

        void AddDomainEvent(IDomainEvent domainEvent);

        void RemoveDomainEvent(IDomainEvent domainEvent);
    }

    /// <summary>
    /// Defines an entity with a single primary key with "Id" property.
    /// </summary>
    public interface IEntity<TKey> : IEntity
    {
        /// <summary>
        /// Unique identifier for this entity.
        /// </summary>
        TKey Id { get; set; }
    }
}
