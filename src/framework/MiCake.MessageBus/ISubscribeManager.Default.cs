using MiCake.Core.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.MessageBus
{
    /// <summary>
    /// Default impl for <see cref="ISubscribeManager"/>
    /// </summary>
    internal class DefaultSubscribeManager : ISubscribeManager
    {
        private ConcurrentDictionary<IMessageSubscriber, CancellationTokenSource> _subscriberTokenDic;
        private readonly IMessageSubscribeFactory _subscribeProvider;
        private readonly ILogger<ISubscribeManager> _logger;

        private bool isDisposed;

        public DefaultSubscribeManager(
            IMessageSubscribeFactory subscribeProvider,
            ILoggerFactory loggerFactory)
        {
            _subscribeProvider = subscribeProvider;
            _logger = loggerFactory.CreateLogger<ISubscribeManager>();
            _subscriberTokenDic = new ConcurrentDictionary<IMessageSubscriber, CancellationTokenSource>();
        }

        /// <summary>
        /// Use <see cref="IMessageSubscribeFactory"/> create a new subscriber,
        /// And save it's Corresponding cancel token source to dic.
        /// </summary>
        public Task<IMessageSubscriber> CreateAsync(CancellationTokenSource cancellationTokenSource)
        {
            var cts = cancellationTokenSource ??
                throw new ArgumentNullException($"The {nameof(cancellationTokenSource)} cannot be null when create new {nameof(IMessageSubscriber)}.");

            var subscriber = _subscribeProvider.CreateSubscriber();
            _subscriberTokenDic.TryAdd(subscriber, cts);

            return Task.FromResult(subscriber);
        }

        public IEnumerable<IMessageSubscriber> GetAllSubscribers()
            => _subscriberTokenDic.Select(s => s.Key);

        /// <summary>
        /// Cancel the subscriber's listening and remove it from the dictionary so that GC can reclaim the resources it occupies
        /// </summary>
        public Task RemoveAsync(IMessageSubscriber messageSubscriber)
        {
            CheckValue.NotNull(messageSubscriber, nameof(messageSubscriber));

            //if no result,return completed directly.
            if (!_subscriberTokenDic.TryGetValue(messageSubscriber, out var cts))
            {
                messageSubscriber.Dispose();
                return Task.CompletedTask;
            }

            //get token and cancel current subscriber.
            try
            {
                cts.Cancel();
            }
            catch (OperationCanceledException canceledException)
            {
                _logger.LogWarning($"{messageSubscriber.ToString()} has been cancelled.There is no need to cancel again." +
                    $"\r\n Exception Message is:{canceledException.Message}");
            }
            finally
            {
                //it is mean release subscribe connection or other resources.
                messageSubscriber.Dispose();

                _subscriberTokenDic.TryRemove(messageSubscriber, out cts);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            //release all subscriber
            foreach (var kvp in _subscriberTokenDic)
            {
                RemoveAsync(kvp.Key).GetAwaiter().GetResult();
            }
            _subscriberTokenDic.Clear();
        }
    }
}
