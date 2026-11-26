using System.Collections.Generic;

namespace MiCake.DDD.Infrastructure.Lifetime
{
    /// <summary>
    /// Tracks domain event instances already dispatched within the current unit of work
    /// scope so controlled save re-entry cycles never dispatch the same event instance twice.
    /// Scoped per unit of work scope; reference identity, not equality, is used.
    /// </summary>
    internal sealed class DomainEventDispatchTracker
    {
        private readonly HashSet<object> _dispatched = new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Records the event as dispatched. Returns false when the same instance was
        /// already dispatched in this unit of work scope.
        /// </summary>
        public bool TryMarkDispatched(object domainEvent) => _dispatched.Add(domainEvent);

        /// <summary>
        /// Removes a previously recorded event so a failed dispatch can be retried.
        /// </summary>
        public void UnmarkDispatched(object domainEvent) => _dispatched.Remove(domainEvent);
    }
}