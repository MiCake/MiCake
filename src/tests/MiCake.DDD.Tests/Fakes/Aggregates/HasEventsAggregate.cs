using MiCake.Cord.Tests.Fakes.DomainEvents;
using MiCake.DDD.Domain;

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
