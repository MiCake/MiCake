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
            RaiseDomainEvent(new CreateOrderEvents(32));
        }
    }
}
