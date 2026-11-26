using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Domain.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Infrastructure.Lifetime
{
    internal class DomainEventDispatchLifetime : IRepositoryPreSaveChanges
    {
        private readonly IEventDispatcher _eventDispatcher;
        private readonly ILogger<DomainEventDispatchLifetime> _logger;
        private readonly DomainEventOptions _options;
        private readonly DomainEventDispatchTracker _dispatchTracker;

        public DomainEventDispatchLifetime(
            IEventDispatcher eventDispatcher, 
            ILoggerFactory loggerFactory,
            IOptions<DomainEventOptions> options,
            DomainEventDispatchTracker dispatchTracker)
        {
            _eventDispatcher = eventDispatcher;
            _logger = loggerFactory.CreateLogger<DomainEventDispatchLifetime>();
            _options = options?.Value ?? new DomainEventOptions();
            _dispatchTracker = dispatchTracker;
        }

        public int Order { get; set; } = -1000;

        public async ValueTask<RepositoryEntityStates> PreSaveChangesAsync(RepositoryEntityStates entityState, object entity, CancellationToken cancellationToken = default)
        {
            if (entity is not IDomainEventAccessor domainEventAccessor)
                return entityState;

            var entityEvents = domainEventAccessor.GetDomainEventsInternal();
            if (entityEvents == null || entityEvents.Count == 0)
                return entityState;

            _logger.LogDebug("Dispatching {Count} domain events for entity {EntityType}",
                entityEvents.Count, entity.GetType().Name);

            var completedEventCount = 0;
            var failedEvents = new List<(IDomainEvent DomainEvent, Exception Error)>();

            foreach (var @event in entityEvents)
            {
                var outcome = await DispatchEventAsync(@event, failedEvents, cancellationToken).ConfigureAwait(false);
                if (outcome == DispatchOutcome.Dispatched)
                {
                    completedEventCount++;
                }
                else if (outcome == DispatchOutcome.FailedStop)
                {
                    _logger.LogWarning(
                        "Stopping domain event dispatch due to error. Completed: {Completed}, Failed: {Failed}",
                        completedEventCount,
                        failedEvents.Count);
                    break;
                }
            }

            if (failedEvents.Count > 0 && 
                _options.OnEventFailure == DomainEventOptions.EventFailureStrategy.ContinueOnError)
            {
                _logger.LogWarning(
                    "Domain event dispatch completed with {FailedCount} failures out of {TotalCount} events",
                    failedEvents.Count,
                    entityEvents.Count);
            }

            return entityState;
        }

        /// <summary>
        /// Dispatches a single domain event, honoring the configured failure strategy.
        /// Failures are recorded into <paramref name="failedEvents"/> (retry eligibility is
        /// restored before recording); a throw-on-error strategy propagates directly.
        /// </summary>
        private async Task<DispatchOutcome> DispatchEventAsync(
            IDomainEvent @event,
            List<(IDomainEvent DomainEvent, Exception Error)> failedEvents,
            CancellationToken cancellationToken)
        {
            if (!_dispatchTracker.TryMarkDispatched(@event))
            {
                // The same event instance was already dispatched in this unit of work
                // scope (for example, during an earlier save cycle).
                _logger.LogDebug("Skipping already dispatched domain event of type {EventType}", @event.GetType().Name);
                return DispatchOutcome.Skipped;
            }

            try
            {
                _logger.LogDebug("Dispatching event {EventType}", @event.GetType().Name);
                await _eventDispatcher.DispatchAsync(@event, cancellationToken);
                return DispatchOutcome.Dispatched;
            }
            catch (Exception ex)
            {
                // A failed dispatch must remain eligible for retry after the unit of
                // work is rolled back, so the tracking record is removed here.
                _dispatchTracker.UnmarkDispatched(@event);
                _logger.LogError(ex, "Failed to dispatch domain event of type {EventType}", @event.GetType().Name);

                if (_options.OnEventFailure == DomainEventOptions.EventFailureStrategy.ThrowOnError)
                {
                    throw new DomainEventException(
                        $"Failed to dispatch domain event of type {@event.GetType().Name}. See inner exception for details.",
                        @event,
                        ex);
                }

                // Recorded only for the non-throwing strategies (StopOnError / ContinueOnError),
                // where the batch continues and the failure list is later surfaced to the caller.
                failedEvents.Add((@event, ex));
                return _options.OnEventFailure == DomainEventOptions.EventFailureStrategy.StopOnError
                    ? DispatchOutcome.FailedStop
                    : DispatchOutcome.FailedContinue;
            }
        }

        /// <summary>
        /// Outcome of dispatching a single domain event.
        /// </summary>
        private enum DispatchOutcome
        {
            /// <summary>The event was already dispatched in this unit of work scope.</summary>
            Skipped,

            /// <summary>The event was dispatched successfully.</summary>
            Dispatched,

            /// <summary>The dispatch failed and the batch should stop (StopOnError).</summary>
            FailedStop,

            /// <summary>The dispatch failed and the batch should continue (ContinueOnError).</summary>
            FailedContinue
        }
    }
}
