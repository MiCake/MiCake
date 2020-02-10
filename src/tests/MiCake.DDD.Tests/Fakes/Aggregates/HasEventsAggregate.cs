using MiCake.DDD.Domain;
using MiCake.DDD.Tests.Fakes.DomainEvents;

namespace MiCake.DDD.Tests.Fakes.Aggregates
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
