using MiCake.DDD.Domain;

namespace MiCake.DDD.Tests.Fakes.Entities
{
    public class EntityA : Entity
    {
        // Public wrapper for testing purposes
        public void AddDomainEventPublic(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
    }
}
