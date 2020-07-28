using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus.RabbitMQ
{
    internal class RabbitMQMessageSubscriber : IMessageSubscriber
    {
        public RabbitMQMessageSubscriber()
        {
        }

        public Task CommitAsync(object sender, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Listen()
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(MessageDeliveryOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
