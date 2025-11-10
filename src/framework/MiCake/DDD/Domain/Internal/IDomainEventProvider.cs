using System.Collections.Generic;

namespace MiCake.DDD.Domain.Internal
{
    /// <summary>
    /// Internal interface for domain event providers
    /// </summary>
    public interface IDomainEventProvider
    {
        /// <summary>
        /// Gets all domain events as a read-only collection
        /// </summary>
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    }
}
