using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internel;
using MiCake.DDD.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
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
            if (entity is IDomianEventProvider domianEventProvider)
            {
                var entityEvents = domianEventProvider.GetDomainEvents();
                var completedEventCount = 0;

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
        }

        public async Task PreSaveChangesAsync(RepositoryEntityState entityState, object entity, CancellationToken cancellationToken = default)
        {
            if (entity is IDomianEventProvider domianEventProvider)
            {
                var entityEvents = domianEventProvider.GetDomainEvents();
                var completedEventCount = 0;

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
        }
    }
}
