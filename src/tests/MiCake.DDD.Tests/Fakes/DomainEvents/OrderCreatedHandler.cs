using MiCake.DDD.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Cord.Tests.Fakes.DomainEvents
{
    public class OrderCreatedHandler : IDomainEventHandler<CreateOrderEvents>
    {
        public static int HanlderChangedValue = 500;

        public Task HandleAysnc(CreateOrderEvents domainEvent, CancellationToken cancellationToken = default)
        {
            domainEvent.OrderID = HanlderChangedValue;

            return Task.CompletedTask;
        }
    }
}
