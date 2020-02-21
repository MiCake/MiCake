using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Store;
using MiCake.EntityFrameworkCore.LifeTime;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Extensions.DDD
{
    internal class DomainEventsEFRepositoryLifetime : IEfRepositoryPreSaveChanges
    {
        private IEventDispatcher _eventDispatcher;
        public DomainEventsEFRepositoryLifetime(IEventDispatcher eventDispatcher)
        {
            _eventDispatcher = eventDispatcher;
        }

        public void PreSaveChanges(RepositoryEntityState entityState, object entity)
        {
            var currentEntity = GetActualEntity(entity);

            if (currentEntity != null)
            {
                var entityEvents = currentEntity.DomainEvents;

                foreach (var @event in entityEvents)
                {
                    try
                    {
                        _eventDispatcher.Dispatch(@event);

                        currentEntity.RemoveDomainEvent(@event);
                    }
                    catch { }
                }

                if (entityEvents.Count > 0)
                {
                    //count is not zero. prove the existence of failed events
                }
            }
        }

        public async Task PreSaveChangesAsync(RepositoryEntityState entityState, object entity, CancellationToken cancellationToken = default)
        {
            // this entity may be storageModel
            var currentEntity = GetActualEntity(entity);

            if (currentEntity != null)
            {
                var entityEvents = currentEntity.DomainEvents;

                foreach (var @event in entityEvents)
                {
                    try
                    {
                        await _eventDispatcher.DispatchAsync(@event);

                        currentEntity.RemoveDomainEvent(@event);
                    }
                    catch { }
                }

                if (entityEvents.Count > 0)
                {
                    //count is not zero. prove the existence of failed events
                }
            }
        }

        private IEntity GetActualEntity(object entity)
        {
            if (!(entity is IEntity))
                return null;

            if (entity is IStorageModel)
            {
                return null;
            }
            else
            {
                return (IEntity)entity;
            }
        }
    }
}
