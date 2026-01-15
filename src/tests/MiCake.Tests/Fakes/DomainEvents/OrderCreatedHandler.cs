using MiCake.DDD.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Tests.Fakes.DomainEvents
{
    public class OrderCreatedHandler : IDomainEventHandler<CreateOrderEvents>
    {
        public static int HanlderChangedValue = 500;

        public Task HandleAsync(CreateOrderEvents domainEvent, CancellationToken cancellationToken = default)
        {
            domainEvent.OrderID = OrderCreatedHandler.HanlderChangedValue;

            return Task.CompletedTask;
        }
    }
}
