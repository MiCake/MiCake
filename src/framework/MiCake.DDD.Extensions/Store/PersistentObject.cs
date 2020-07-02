using MiCake.Core.Data;
using MiCake.Core.Util;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Internel;
using MiCake.DDD.Extensions.Store.Mapping;
using System.Collections.Generic;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Defines an persistent object.
    /// Mabey you need use generic type <see cref="IPersistentObject{TEntity}"/>
    /// </summary>
    public interface IPersistentObject : IDomainEventProvider, INeedParts<IPersistentObjectMapper>
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
    /// <typeparam name="TPersistentObject"></typeparam>
    public abstract class PersistentObject<TEntity, TPersistentObject> : IPersistentObject<TEntity>
        where TEntity : IAggregateRoot
        where TPersistentObject : IPersistentObject<TEntity>
    {
        private List<IDomainEvent> _domainEvents;

        protected IPersistentObjectMapConfig<TEntity, TPersistentObject> MapConfig { get; private set; }

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
        /// Configure relationship mapping between <see cref="IEntity"/> and <see cref="TPersistentObject"/>.
        /// </summary>
        public abstract void ConfigureMapping();

        void INeedParts<IPersistentObjectMapper>.SetParts(IPersistentObjectMapper parts)
        {
            CheckValue.NotNull(parts, nameof(IPersistentObjectMapper));
            MapConfig = parts.Create<TEntity, TPersistentObject>();
        }
    }
}
