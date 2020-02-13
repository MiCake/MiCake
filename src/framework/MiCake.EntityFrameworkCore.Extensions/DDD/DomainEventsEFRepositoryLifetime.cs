using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Extensions;
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

        private IEntity GetActualEntity(object storeEntityType)
        {
            var currentEntity = storeEntityType as IEntity;

            if (currentEntity != null)
            {
                return currentEntity;
            }
            else if (EntitySnapshotStore.GetEntity(storeEntityType, out currentEntity))
            {
                EntitySnapshotStore.RemoveMapping(storeEntityType);
                return currentEntity;
            }

            return null;
        }
    }
}
