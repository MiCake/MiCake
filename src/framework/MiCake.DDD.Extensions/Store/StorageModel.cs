using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Internel;
using System.Collections.Generic;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Defines an storage model.
    /// Mabey you need use generic type <see cref="IStorageModel{TEntity}"/>
    /// </summary>
    public interface IStorageModel : IDomianEventProvider
    {
        IStorageModel AddDomainEvents(List<IDomainEvent> domainEvents);

        IStorageModel ClearDomainEvents();

        /// <summary>
        /// Configure relationship mapping between <see cref="IEntity"/> and storage model
        /// </summary>
        void ConfigureMapping();
    }

    public interface IStorageModel<TEntity> : IStorageModel
        where TEntity : IAggregateRoot
    {
    }

    /// <summary>
    /// Base class of storage model
    /// You should configure relationship mapping between <see cref="IEntity"/> and storage model by override <see cref="ConfigureMapping"/>
    /// </summary>
    /// <typeparam name="TEntity"><see cref="IEntity"/></typeparam>
    public abstract class StorageModel<TEntity> : IStorageModel<TEntity>
        where TEntity : IAggregateRoot
    {
        private List<IDomainEvent> _domainEvents;

        public IStorageModel AddDomainEvents(List<IDomainEvent> domainEvents)
        {
            _domainEvents = domainEvents;
            return this;
        }

        public IStorageModel ClearDomainEvents()
        {
            _domainEvents = null;
            return this;
        }

        public List<IDomainEvent> GetDomainEvents()
            => _domainEvents;

        /// <summary>
        /// Configure relationship mapping between <see cref="IEntity"/> and storage model.
        /// </summary>
        public abstract void ConfigureMapping();


    }
}
