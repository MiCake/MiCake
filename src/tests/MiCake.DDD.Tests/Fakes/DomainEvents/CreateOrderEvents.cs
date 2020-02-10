using MiCake.DDD.Domain;

namespace MiCake.DDD.Tests.Fakes.DomainEvents
{
    public class CreateOrderEvents : DomainEvent
    {
        public int OrderID { get;  set; }

        public CreateOrderEvents(int id)
        {
            OrderID = id;
        }
    }
}
