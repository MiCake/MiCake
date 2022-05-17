using MiCake.DDD.Domain;
using MiCake.DDD.Tests.Fakes.DomainEvents;

namespace MiCake.Cord.Tests.Fakes.Aggregates
{
    public class HasEventsAggregate : AggregateRoot
    {
        public HasEventsAggregate()
        {
        }

        public void OneAddEventCase()
        {
            AddDomainEvent(new CreateOrderEvents(32));
        }
    }
}
