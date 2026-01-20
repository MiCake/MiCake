using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Domain.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Base class for entities with integer identity.
    /// </summary>
    [Serializable]
    public abstract class Entity : Entity<int>
    {
    }

    /// <summary>
    /// Base class for domain entities following Domain-Driven Design principles.
    /// Entities are compared by their identity (Id) rather than their property values.
    /// </summary>
    /// <typeparam name="TKey">The type of the entity identifier</typeparam>
    [Serializable]
    public abstract class Entity<TKey> : IEntity<TKey>, IDomainEventAccessor where TKey : notnull
    {
        private List<IDomainEvent>? _domainEvents;

        /// <summary>
        /// Gets or init the unique identifier for this entity.
        /// Using init ensures immutability after construction.
        /// </summary>
        public virtual TKey Id { get; init; } = default!;

        /// <summary>
        /// Gets all pending domain events for this entity as a read-only collection.
        /// External code cannot modify the collection directly.
        /// </summary>
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly() ?? [];

        /// <summary>
        /// Adds a domain event to the entity.
        /// The event will be dispatched when changes are saved to the repository.
        /// </summary>
        /// <param name="domainEvent">The domain event to add</param>
        /// <exception cref="ArgumentNullException">Thrown when domainEvent is null</exception>
        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            _domainEvents ??= new List<IDomainEvent>(4);

            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Clears all pending domain events.
        /// This is typically called after events have been dispatched.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents?.Clear();
        }

        /// <summary>
        /// Gets all pending domain events for this entity (internal use).
        /// Explicit interface implementation to hide from public API.
        /// </summary>
        List<IDomainEvent> IDomainEventAccessor.GetDomainEventsInternal() => _domainEvents ?? [];

        /// <summary>
        /// Determines whether the specified object is equal to the current entity.
        /// Entities are equal if they have the same type and Id.
        /// </summary>
        /// <param name="obj">The object to compare with the current entity</param>
        /// <returns>True if the specified object is equal to the current entity; otherwise, false</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not Entity<TKey>)
            {
                return false;
            }

            //Same instances must be considered as equal
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var equalEntity = (Entity<TKey>)obj;

            if (EntityHelper.HasDefaultId(this) && EntityHelper.HasDefaultId(equalEntity))
            {
                return false;
            }

            //Compare type
            var typeOfThis = GetType().GetTypeInfo();
            var typeOfOther = equalEntity.GetType().GetTypeInfo();
            if (!typeOfThis.IsAssignableFrom(typeOfOther) && !typeOfOther.IsAssignableFrom(typeOfThis))
            {
                return false;
            }

            return Id.Equals(equalEntity.Id);
        }

        public override int GetHashCode()
        {
            if (Id is null)
            {
                return 0;
            }

            return Id.GetHashCode();
        }

        public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
        {
            return !(left == right);
        }
    }
}
