using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus
{
    /// <summary>
    /// Default impl for <see cref="ISubscribeManager"/>
    /// </summary>
    internal class DefaultSubscribeManager : ISubscribeManager
    {
        public DefaultSubscribeManager()
        {
        }

        public Task<IMessageSubscriber> CreateAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMessageSubscriber> GetAllSubscribers()
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(IMessageSubscriber messageSubscriber, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(IMessageSubscriber messageSubscriber)
        {
            throw new NotImplementedException();
        }
    }
}
