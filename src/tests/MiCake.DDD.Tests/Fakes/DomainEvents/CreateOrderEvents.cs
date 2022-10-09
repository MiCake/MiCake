using MiCake.DDD.Domain;

namespace MiCake.Cord.Tests.Fakes.DomainEvents
{
    public class CreateOrderEvents : DomainEvent
    {
        public int OrderID { get; set; }

        public CreateOrderEvents(int id)
        {
            OrderID = id;
        }
    }
}
