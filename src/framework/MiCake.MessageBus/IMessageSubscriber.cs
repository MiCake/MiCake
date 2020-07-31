using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus
{
    /// <summary>
    /// Define a message subscriber that can receive message from <see cref="IMessageBus"/>.
    /// </summary>
    public interface IMessageSubscriber : IDisposable
    {
        /// <summary>
        /// Subscribe to the message queue by global config.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SubscribeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to the message queue by appoint options.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SubscribeAsync(MessageDeliveryOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Manual commit message,indicate that acknowledge one or more message(s).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CommitAsync(object sender, CancellationToken cancellationToken = default);

        /// <summary>
        /// Keep listen to receive bus message(s).
        /// </summary>
        Task ListenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a handler to receive message form subscriber.
        /// </summary>
        /// <param name="handler"><see cref="SubscriberMessageReceived"/>A delegate for receive message.</param>
        void AddReceivedHandler(SubscriberMessageReceived handler);
    }
}
