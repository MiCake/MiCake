using System.Collections.Generic;

namespace MiCake.DDD.Domain.Internel
{
    public interface IDomianEventProvider
    {
        /// <summary>
        /// Get All DomainEvents
        /// </summary>
        List<IDomainEvent> GetDomainEvents();
    }
}
