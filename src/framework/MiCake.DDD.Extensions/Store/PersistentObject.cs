using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Internel;
using System.Collections.Generic;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Defines an persistent object.
    /// Mabey you need use generic type <see cref="IPersistentObject{TEntity}"/>
    /// </summary>
    public interface IPersistentObject : IDomainEventProvider
    {
        IPersistentObject AddDomainEvents(List<IDomainEvent> domainEvents);

        IPersistentObject ClearDomainEvents();

        /// <summary>
        /// Configure relationship mapping between <see cref="IEntity"/> and persistent object.
        /// </summary>
        void ConfigureMapping();
    }

    public interface IPersistentObject<TEntity> : IPersistentObject
        where TEntity : IAggregateRoot
    {
    }

    /// <summary>
    /// Base class of persistent object.
    /// You should configure relationship mapping between <see cref="IEntity"/> and persistent object by override <see cref="ConfigureMapping"/>
    /// </summary>
    /// <typeparam name="TEntity"><see cref="IEntity"/></typeparam>
    public abstract class PersistentObject<TEntity> : IPersistentObject<TEntity>
        where TEntity : IAggregateRoot
    {
        private List<IDomainEvent> _domainEvents;

        public IPersistentObject AddDomainEvents(List<IDomainEvent> domainEvents)
        {
            _domainEvents = domainEvents;
            return this;
        }

        public IPersistentObject ClearDomainEvents()
        {
            _domainEvents = null;
            return this;
        }

        public List<IDomainEvent> GetDomainEvents()
            => _domainEvents;

        /// <summary>
        /// Configure relationship mapping between <see cref="IEntity"/> and persistent object.
        /// </summary>
        public abstract void ConfigureMapping();
    }
}
