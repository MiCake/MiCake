using System.Collections.Generic;

namespace MiCake.DDD.Domain.Internal
{
    /// <summary>
    /// Internal interface for accessing domain events list (framework use only)
    /// </summary>
    internal interface IDomainEventAccessor
    {
        /// <summary>
        /// Gets the internal domain events list for modification by the framework
        /// </summary>
        List<IDomainEvent> GetDomainEventsInternal();
    }
}
