using System.Collections.Concurrent;

namespace MiCake.DDD.Domain.EventDispatch
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private static readonly ConcurrentDictionary<Type, DomainEventHandlerWrapper> _domainEventHandlers = new();

        private readonly IServiceProvider _serviceProvider;

        public DomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
            where TDomainEvent : IDomainEvent
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
                factory => (DomainEventHandlerWrapper)Activator.CreateInstance(typeof(DomainEventHandlerWrapperImp<>).MakeGenericType(domainEventType))!);

            return handler.Handle(domainEvent, _serviceProvider, PublishCore, cancellationToken);
        }
    }
}
