using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus
{
    /// <summary>
    /// Define a manager to manange <see cref="IMessageSubscriber"/>
    /// </summary>
    public interface ISubscribeManager : IDisposable
    {
        /// <summary>
        /// Get all registered <see cref="IMessageSubscriber"/>(s).
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMessageSubscriber> GetAllSubscribers();

        /// <summary>
        /// Create a new <see cref="IMessageSubscriber"/>.
        /// </summary>
        /// <param name="cancellationTokenSource">token source that used to cancel subscriber listen.</param>
        /// <returns></returns>
        Task<IMessageSubscriber> CreateAsync(CancellationTokenSource cancellationTokenSource);

        /// <summary>
        /// Remove current <see cref="IMessageSubscriber"/>.
        /// It's mean that the subscription will be unsubscribed and the links occupied by that subscriber will be released.
        /// </summary>
        /// <param name="messageSubscriber">need remove <see cref="IMessageSubscriber"/></param>
        Task RemoveAsync(IMessageSubscriber messageSubscriber);
    }
}
