using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using Microsoft.Extensions.Logging;

namespace MiCake.Cord.Lifetime
{
    internal class DomainEventsRepositoryLifetime : IRepositoryPreSaveChanges
    {
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly ILogger<DomainEventsRepositoryLifetime> _logger;

        public DomainEventsRepositoryLifetime(IDomainEventDispatcher eventDispatcher, ILoggerFactory loggerFactory)
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

                List<Exception> errors = new();

                foreach (var @event in entityEvents)
                {
                    try
                    {
                        await _eventDispatcher.DispatchAsync(@event, cancellationToken);
                        completedEventCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }

                if (errors.Count > 0)
                {
                    _logger.LogError("Failed to dispatch {eventCount} events.", errors.Count);
                    throw new AggregateException(errors);
                }
            }
            return entityState;
        }
    }
}
