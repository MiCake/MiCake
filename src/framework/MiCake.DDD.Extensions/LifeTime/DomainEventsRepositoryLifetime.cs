using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.Lifetime
{
    internal class DomainEventsRepositoryLifetime : IRepositoryPreSaveChanges
    {
        private IEventDispatcher _eventDispatcher;
        public DomainEventsRepositoryLifetime(IEventDispatcher eventDispatcher)
        {
            _eventDispatcher = eventDispatcher;
        }

        public int Order { get; set; } = -1000;

        public RepositoryEntityState PreSaveChanges(RepositoryEntityState entityState, object entity)
        {
            if (entity is IDomainEventProvider domainEventProvider)
            {
                var entityEvents = domainEventProvider.GetDomainEvents();
                var completedEventCount = 0;

                if (entityEvents == null || entityEvents.Count == 0)
                    return entityState;

                foreach (var @event in entityEvents)
                {
                    try
                    {
                        _eventDispatcher.Dispatch(@event);
                        completedEventCount++;
                    }
                    catch { }
                }

                if (completedEventCount != entityEvents.Count)
                {
                    //count is not equal. prove the existence of failed events
                }
            }

            return entityState;
        }

        public async ValueTask<RepositoryEntityState> PreSaveChangesAsync(RepositoryEntityState entityState, object entity, CancellationToken cancellationToken = default)
        {
            if (entity is IDomainEventProvider domainEventProvider)
            {
                var entityEvents = domainEventProvider.GetDomainEvents();
                var completedEventCount = 0;

                if (entityEvents == null || entityEvents.Count == 0)
                    return entityState;

                foreach (var @event in entityEvents)
                {
                    try
                    {
                        await _eventDispatcher.DispatchAsync(@event);
                        completedEventCount++;
                    }
                    catch { }
                }

                if (completedEventCount != entityEvents.Count)
                {
                    //count is not equal. prove the existence of failed events
                }
            }
            return entityState;
        }
    }
}
