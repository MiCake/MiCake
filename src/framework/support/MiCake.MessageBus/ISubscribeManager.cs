﻿namespace MiCake.MessageBus
{
    /// <summary>
    /// Define a manager to manange <see cref="IMessageSubscriber"/>
    /// </summary>
    public interface ISubscribeManager : IAsyncDisposable
    {
        /// <summary>
        /// Get all registered <see cref="IMessageSubscriber"/>(s).
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMessageSubscriber> GetAllSubscribers();

        /// <summary>
        /// Create a new <see cref="IMessageSubscriber"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IMessageSubscriber> CreateAsync(MessageSubscriberOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove current <see cref="IMessageSubscriber"/>.
        /// It's mean that the subscription will be unsubscribed and the links occupied by that subscriber will be released.
        /// </summary>
        /// <param name="messageSubscriber">need remove <see cref="IMessageSubscriber"/></param>
        Task RemoveAsync(IMessageSubscriber messageSubscriber);
    }
}
