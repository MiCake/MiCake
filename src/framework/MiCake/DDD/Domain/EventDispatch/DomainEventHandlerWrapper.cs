using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain.EventDispatch
{
    internal abstract class DomainEventHandlerWrapper
    {
        public abstract Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken, IServiceProvider serviceProvider, Func<IEnumerable<Func<Task>>, Task> publish);
    }

    internal class DomainEventHandlerWrapperImp<TDomainEvent> : DomainEventHandlerWrapper
        where TDomainEvent : IDomainEvent
    {
        public override Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken, IServiceProvider serviceProvider, Func<IEnumerable<Func<Task>>, Task> publish)
        {
            var handlers = serviceProvider
                 .GetServices<IDomainEventHandler<TDomainEvent>>()
                 .Select(x => new Func<Task>(() => x.HandleAsync((TDomainEvent)domainEvent, cancellationToken)));

            return publish(handlers);
        }
    }
}
