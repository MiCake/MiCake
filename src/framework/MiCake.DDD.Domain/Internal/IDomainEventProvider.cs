using System.Collections.Generic;

namespace MiCake.DDD.Domain.Internal
{
    public interface IDomainEventProvider
    {
        /// <summary>
        /// Get All DomainEvents
        /// </summary>
        List<IDomainEvent> GetDomainEvents();
    }
}
