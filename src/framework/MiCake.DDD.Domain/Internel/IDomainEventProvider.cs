using System.Collections.Generic;

namespace MiCake.DDD.Domain.Internel
{
    public interface IDomainEventProvider
    {
        /// <summary>
        /// Get All DomainEvents
        /// </summary>
        List<IDomainEvent> GetDomainEvents();
    }
}
