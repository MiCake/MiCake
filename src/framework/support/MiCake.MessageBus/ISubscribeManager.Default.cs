﻿using MiCake.Core.Util;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MiCake.MessageBus
{
    /// <summary>
    /// Default impl for <see cref="ISubscribeManager"/>
    /// </summary>
    internal class DefaultSubscribeManager : ISubscribeManager
    {
        private readonly ConcurrentDictionary<IMessageSubscriber, MessageSubscriberOptions> _subscriberDic;
        private readonly IMessageSubscribeFactory _subscribeFactory;
        private readonly ILogger<ISubscribeManager> _logger;

        private bool isDisposed;

        public DefaultSubscribeManager(
            IMessageSubscribeFactory subscribeFactory,
            ILoggerFactory loggerFactory)
        {
            _subscribeFactory = subscribeFactory;
            _logger = loggerFactory.CreateLogger<ISubscribeManager>();
            _subscriberDic = new ConcurrentDictionary<IMessageSubscriber, MessageSubscriberOptions>();
        }

        /// <summary>
        /// Use <see cref="IMessageSubscribeFactory"/> create a new subscriber,
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IMessageSubscriber> CreateAsync(MessageSubscriberOptions options, CancellationToken cancellationToken = default)
        {
            var subscriber = await _subscribeFactory.CreateSubscriberAsync(options, cancellationToken);
            _subscriberDic.TryAdd(subscriber, options);

            return subscriber;
        }

        public IEnumerable<IMessageSubscriber> GetAllSubscribers()
            => _subscriberDic.Select(s => s.Key);

        /// <summary>
        /// Cancel the subscriber's listening and remove it from the dictionary so that GC can reclaim the resources it occupies
        /// </summary>
        public async Task RemoveAsync(IMessageSubscriber messageSubscriber)
        {
            CheckValue.NotNull(messageSubscriber, nameof(messageSubscriber));

            //if no result,return completed directly.
            if (!_subscriberDic.TryGetValue(messageSubscriber, out _))
            {
                await messageSubscriber.DisposeAsync();
            }

            _subscriberDic.TryRemove(messageSubscriber, out _);
            await messageSubscriber.DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            //release all subscriber
            foreach (var kvp in _subscriberDic)
            {
                await RemoveAsync(kvp.Key);
            }
            _subscriberDic.Clear();
        }
    }
}
