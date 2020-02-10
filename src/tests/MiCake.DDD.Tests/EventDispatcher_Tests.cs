using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Tests.Fakes.Aggregates;
using MiCake.DDD.Tests.Fakes.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace MiCake.DDD.Tests
{
    public class EventDispatcher_Tests
    {
        public IServiceCollection Services { get; set; }

        public EventDispatcher_Tests()
        {
            Services = new ServiceCollection();

            Services.AddSingleton<IEventDispatcher, EventDispatcher>();
        }

        [Fact]
        public void AggregateApplyEvent_ShouldFindEvent()
        {
            HasEventsAggregate aggregate = new HasEventsAggregate();
            aggregate.OneAddEventCase();

            var hasEvent = aggregate.DomainEvents.First();

            Assert.NotNull(hasEvent);
            Assert.IsType<CreateOrderEvents>(hasEvent);
        }

        [Fact]
        public void EventDisptcher_ShouldCallHandler()
        {
            var expectedResult = OrderCreatedHandler.HanlderChangedValue;
            Services.AddTransient<IDomainEventHandler<CreateOrderEvents>, OrderCreatedHandler>();

            var disptcher = Services.BuildServiceProvider().GetService<IEventDispatcher>();

            HasEventsAggregate aggregate = new HasEventsAggregate();
            aggregate.OneAddEventCase();

            var orignalDomainEvent =(CreateOrderEvents)aggregate.DomainEvents.First();

            disptcher.DispatchAsync(orignalDomainEvent);

            Assert.Equal(expectedResult, orignalDomainEvent.OrderID);
        }

    }
}
