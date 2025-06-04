using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.Lifetime
{
    internal class DomainEventsRepositoryLifetime : IRepositoryPreSaveChanges
    {
        private readonly IEventDispatcher _eventDispatcher;
        private readonly ILogger<DomainEventsRepositoryLifetime> _logger;

        public DomainEventsRepositoryLifetime(IEventDispatcher eventDispatcher, ILoggerFactory loggerFactory)
        {
            _eventDispatcher = eventDispatcher;
            _logger = loggerFactory.CreateLogger<DomainEventsRepositoryLifetime>();
        }

        public int Order { get; set; } = -1000;

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
                        await _eventDispatcher.DispatchAsync(@event, cancellationToken);
                        completedEventCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "There has a error when dispatch domain event.");
                    }
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
