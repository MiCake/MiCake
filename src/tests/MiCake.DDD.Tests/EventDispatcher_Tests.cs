using MiCake.Cord.Tests.Fakes.Aggregates;
using MiCake.Cord.Tests.Fakes.DomainEvents;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace MiCake.Cord.Tests
{
    public class EventDispatcher_Tests
    {
        public IServiceCollection Services { get; set; }

        public EventDispatcher_Tests()
        {
            Services = new ServiceCollection();

            Services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        }

        [Fact]
        public void AggregateApplyEvent_ShouldFindEvent()
        {
            HasEventsAggregate aggregate = new();
            aggregate.OneAddEventCase();

            var hasEvent = aggregate.GetDomainEvents().First();

            Assert.NotNull(hasEvent);
            Assert.IsType<CreateOrderEvents>(hasEvent);
        }

        [Fact]
        public void EventDisptcher_ShouldCallHandler()
        {
            var expectedResult = OrderCreatedHandler.HanlderChangedValue;
            Services.AddTransient<IDomainEventHandler<CreateOrderEvents>, OrderCreatedHandler>();

            var disptcher = Services.BuildServiceProvider().GetService<IDomainEventDispatcher>();

            HasEventsAggregate aggregate = new();
            aggregate.OneAddEventCase();

            var orignalDomainEvent = (CreateOrderEvents)aggregate.GetDomainEvents().First();

            disptcher.DispatchAsync(orignalDomainEvent);

            Assert.Equal(expectedResult, orignalDomainEvent.OrderID);
        }

    }
}
