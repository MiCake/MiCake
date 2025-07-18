﻿using MiCake.DDD.Domain.Helper;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MiCake.DDD.Domain
{
    [Serializable]
    public abstract class Entity : Entity<int>
    {
    }

    [Serializable]
    public abstract class Entity<TKey> : IEntity<TKey>
    {
        public virtual TKey Id { get; set; }

        protected List<IDomainEvent> _domainEvents = [];

        public virtual void AddDomainEvent(IDomainEvent domainEvent)
          => _domainEvents.Add(domainEvent);

        public virtual void RemoveDomainEvent(IDomainEvent domainEvent)
          => _domainEvents.Remove(domainEvent);

        public List<IDomainEvent> GetDomainEvents()
          => _domainEvents;

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
