using MiCake.DDD.Domain.Internal;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Base interface for entities
    /// </summary>
    public interface IEntity : IDomainEventProvider
    {
        /// <summary>
        /// Clears all pending domain events
        /// </summary>
        void ClearDomainEvents();
    }

    /// <summary>
    /// Defines an entity with a single primary key with "Id" property.
    /// </summary>
    /// <typeparam name="TKey">The type of the entity identifier</typeparam>
    public interface IEntity<TKey> : IEntity where TKey : notnull
    {
        /// <summary>
        /// Unique identifier for this entity.
        /// Using init ensures immutability after construction.
        /// </summary>
        TKey Id { get; init; }
    }
}
