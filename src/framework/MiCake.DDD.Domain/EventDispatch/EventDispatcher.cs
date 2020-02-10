using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain.EventDispatch
{
    public class EventDispatcher : IEventDispatcher
    {
        private static readonly ConcurrentDictionary<Type, DomainEventHandlerWrapper> _domainEventHandlers = new ConcurrentDictionary<Type, DomainEventHandlerWrapper>();

        private IServiceProvider _serviceProvider;

        public EventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Dispatch<TDomainEvent>(TDomainEvent domainEvent)
            where TDomainEvent : IDomainEvent
        {
            DispatchAsync(domainEvent).GetAwaiter().GetResult();
        }

        public Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
            where TDomainEvent :IDomainEvent
        {
            if (domainEvent == null)
                return Task.CompletedTask;

            return PublishDomainEvents(domainEvent, cancellationToken);
        }

        protected virtual async Task PublishCore(IEnumerable<Func<Task>> allHandlers)
        {
            foreach (var handler in allHandlers)
            {
                await handler().ConfigureAwait(false);
            }
        }

        private Task PublishDomainEvents(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var domainEventType = domainEvent.GetType();
            var handler = _domainEventHandlers.GetOrAdd(domainEventType,
                factory => (DomainEventHandlerWrapper)Activator.CreateInstance(typeof(DomainEventHandlerWrapperImp<>).MakeGenericType(domainEventType)));

            return handler.Handle(domainEvent, cancellationToken, _serviceProvider, PublishCore);
        }
    }
}
