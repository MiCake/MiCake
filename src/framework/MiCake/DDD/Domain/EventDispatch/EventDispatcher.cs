using MiCake.Util.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain.EventDispatch
{
    public class EventDispatcher : IEventDispatcher
    {
        private static readonly ConcurrentDictionary<Type, IDomainEventHandler> _domainEventHandlers = new();

        private readonly IServiceProvider _serviceProvider;

        public EventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
            where TDomainEvent : IDomainEvent
        {
            if (domainEvent is null)
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
                eventType => (IDomainEventHandler)CompiledActivator.CreateInstance(typeof(DomainEventHandlerWrapperImp<>).MakeGenericType(eventType)));

            return handler.Handle(domainEvent, _serviceProvider, PublishCore, cancellationToken);
        }
    }
}
