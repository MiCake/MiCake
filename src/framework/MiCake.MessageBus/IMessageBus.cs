using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus
{
    /// <summary>
    /// A message bus that responsible for sending messages and creating subscriptions.
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Send a message to bus.
        /// </summary>
        /// <param name="message">message info.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SendAsync(object message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a message to bus with some optional headers.
        /// </summary>
        /// <param name="message">message info.</param>
        /// <param name="headers">some header info.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SendAsync(object message, Dictionary<string, string> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a message to bus with some optional headers and appoint options.
        /// </summary>
        /// <param name="message">message info.</param>
        /// <param name="headers">some header info.</param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SendAsync(object message, Dictionary<string, string> headers, MessageDeliveryOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a <see cref="IMessageSubscriber"/> use to subsricbe bus message.
        /// Specifies that the cancellationtoken is used to cancel the subscriber's action.
        /// </summary>
        /// <param name="cancellationToken">Unsubscribed token</param>
        /// <returns></returns>
        Task<IMessageSubscriber> CreateSubscriberAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Cancel the current subscriber and release its resources.
        /// </summary>
        /// <param name="messageSubscriber">need cancel <see cref="IMessageSubscriber"/></param>
        /// <returns></returns>
        Task CancelSubscribeAsync(IMessageSubscriber messageSubscriber);
    }
}
