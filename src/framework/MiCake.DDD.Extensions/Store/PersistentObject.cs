using MiCake.Core.Data;
using MiCake.Core.Util;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Internal;
using MiCake.DDD.Extensions.Store.Mapping;
using System.Collections.Generic;
using System.Diagnostics;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Defines an persistent object.
    /// Mabey you need use generic type <see cref="IPersistentObject{TKey, TEntity}"/>
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

    public interface IPersistentObject<TKey, TEntity> : IPersistentObject
        where TEntity : IAggregateRoot
    {
        TKey Id { get; set; }
    }

    /// <summary>
    /// Base class of persistent object.
    /// You should configure relationship mapping between <see cref="IAggregateRoot"/> and persistent object by override <see cref="ConfigureMapping"/>
    /// </summary>
    /// <typeparam name="TAggregateRoot"><see cref="IEntity"/></typeparam>
    /// <typeparam name="TSelf">self type</typeparam>
    /// <typeparam name="TKey">unique key type.</typeparam>
    public abstract class PersistentObject<TKey, TAggregateRoot, TSelf> : IPersistentObject<TKey, TAggregateRoot>, IHasAccessor<IPersistentObjectMapConfig>
        where TAggregateRoot : IAggregateRoot<TKey>
        where TSelf : IPersistentObject<TKey, TAggregateRoot>
    {
        private List<IDomainEvent> _domainEvents;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected IPersistentObjectMapConfig<TKey, TAggregateRoot, TSelf> MapConfiger;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IPersistentObjectMapConfig IHasAccessor<IPersistentObjectMapConfig>.Instance => MapConfiger;

        public TKey Id { get; set; }

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
        /// Configure relationship mapping between <see cref="IEntity"/> and <see cref="IPersistentObject"/>.
        /// </summary>
        public abstract void ConfigureMapping();

        void INeedParts<IPersistentObjectMapper>.SetParts(IPersistentObjectMapper parts)
        {
            CheckValue.NotNull(parts, nameof(parts));
            MapConfiger = parts.Create<TKey, TAggregateRoot, TSelf>();
        }
    }
}
