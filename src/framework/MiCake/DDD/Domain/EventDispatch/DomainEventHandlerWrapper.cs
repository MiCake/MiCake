using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Domain.EventDispatch
{
    internal interface IDomainEventHandler
    {
        Task Handle(IDomainEvent domainEvent, IServiceProvider serviceProvider, Func<IEnumerable<Func<Task>>, Task> publish, CancellationToken cancellationToken);
    }

    internal class DomainEventHandlerWrapperImp<TDomainEvent> : IDomainEventHandler
        where TDomainEvent : IDomainEvent
    {
        public Task Handle(IDomainEvent domainEvent, IServiceProvider serviceProvider, Func<IEnumerable<Func<Task>>, Task> publish, CancellationToken cancellationToken)
        {
            var handlers = serviceProvider
                 .GetServices<IDomainEventHandler<TDomainEvent>>()
                 .Select(x => new Func<Task>(() => x.HandleAsync((TDomainEvent)domainEvent, cancellationToken)));

            return publish(handlers);
        }
    }
}
