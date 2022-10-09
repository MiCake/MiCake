using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Domain.EventDispatch
{
    internal abstract class DomainEventHandlerWrapper
    {
        public abstract Task Handle(IDomainEvent domainEvent, IServiceProvider serviceProvider, Func<IEnumerable<Func<Task>>, Task> publish, CancellationToken cancellationToken);
    }

    internal class DomainEventHandlerWrapperImp<TDomainEvent> : DomainEventHandlerWrapper
        where TDomainEvent : IDomainEvent
    {
        public override Task Handle(IDomainEvent domainEvent, IServiceProvider serviceProvider, Func<IEnumerable<Func<Task>>, Task> publish, CancellationToken cancellationToken)
        {
            var handlers = serviceProvider
                 .GetServices<IDomainEventHandler<TDomainEvent>>()
                 .Select(x => new Func<Task>(() => x.HandleAysnc((TDomainEvent)domainEvent, cancellationToken)));

            return publish(handlers);
        }
    }
}
