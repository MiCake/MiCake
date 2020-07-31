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
        /// Publish a message to bus.
        /// </summary>
        /// <param name="message">message info.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task PublishAsync(object message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publish a message to bus with some optional headers.
        /// </summary>
        /// <param name="message">message info.</param>
        /// <param name="headers">some header info.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task PublishAsync(object message, Dictionary<string, string> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publish a message to bus with some optional headers and appoint options.
        /// </summary>
        /// <param name="message">message info.</param>
        /// <param name="headers">some header info.</param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task PublishAsync(object message, Dictionary<string, string> headers, MessageDeliveryOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a <see cref="IMessageSubscriber"/> use to subsricbe bus message.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IMessageSubscriber> CreateSubscriberAsync(MessageSubscriberOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel the current subscriber and release its resources.
        /// </summary>
        /// <param name="messageSubscriber">need cancel <see cref="IMessageSubscriber"/></param>
        /// <returns></returns>
        Task CancelSubscribeAsync(IMessageSubscriber messageSubscriber);
    }
}
