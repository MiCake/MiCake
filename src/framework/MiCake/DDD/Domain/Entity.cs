using MiCake.DDD.Domain.Helper;
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
    public abstract class Entity<TKey> : IEntity<TKey>
    {
        /// <summary>
        /// Gets or sets the unique identifier for this entity.
        /// </summary>
        public virtual TKey Id { get; set; }

        protected List<IDomainEvent> _domainEvents = [];

        /// <summary>
        /// Adds a domain event to the entity.
        /// The event will be dispatched when changes are saved to the repository.
        /// </summary>
        /// <param name="domainEvent">The domain event to add</param>
        public virtual void AddDomainEvent(IDomainEvent domainEvent)
          => _domainEvents.Add(domainEvent);

        /// <summary>
        /// Removes a domain event from the entity.
        /// </summary>
        /// <param name="domainEvent">The domain event to remove</param>
        public virtual void RemoveDomainEvent(IDomainEvent domainEvent)
          => _domainEvents.Remove(domainEvent);

        /// <summary>
        /// Gets all pending domain events for this entity.
        /// </summary>
        /// <returns>A list of domain events</returns>
        /// <summary>
        /// Gets all pending domain events for this entity.
        /// </summary>
        /// <returns>A list of domain events</returns>
        public List<IDomainEvent> GetDomainEvents()
          => _domainEvents;

        /// <summary>
        /// Determines whether the specified object is equal to the current entity.
        /// Entities are equal if they have the same type and Id.
        /// </summary>
        /// <param name="obj">The object to compare with the current entity</param>
        /// <returns>True if the specified object is equal to the current entity; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Entity<TKey>))
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
            if (Id == null)
            {
                return 0;
            }

            return Id.GetHashCode();
        }

        public static bool operator ==(Entity<TKey> left, Entity<TKey> right)
        {
            if (Equals(left, null))
            {
                return Equals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TKey> left, Entity<TKey> right)
        {
            return !(left == right);
        }
    }
}
