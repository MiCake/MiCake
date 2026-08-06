using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests
{
    public class DomainEventDispatchLifetime_Tests
    {
        private class TestDomainEvent : DomainEvent
        {
        }

        private class TestEntity : Entity<int>
        {
            public void Raise(TestDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
        }

        [Fact]
        public async Task SameEventInstance_AcrossReentryCycles_IsDispatchedOnce()
        {
            var dispatcher = new Mock<IEventDispatcher>();
            dispatcher.Setup(d => d.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var lifetime = new DomainEventDispatchLifetime(
                dispatcher.Object,
                NullLoggerFactory.Instance,
                Options.Create(new DomainEventOptions()),
                new DomainEventDispatchTracker());

            var entity = new TestEntity();
            var domainEvent = new TestDomainEvent();
            entity.Raise(domainEvent);

            // Cycle 1: the event is dispatched.
            await lifetime.PreSaveChangesAsync(RepositoryEntityStates.Added, entity, CancellationToken.None);

            // Cycle 2: the same instance is raised again by a lifecycle handler; it must
            // not be dispatched a second time.
            entity.Raise(domainEvent);
            await lifetime.PreSaveChangesAsync(RepositoryEntityStates.Added, entity, CancellationToken.None);

            dispatcher.Verify(d => d.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FailedDispatch_RemainsEligibleForRetry()
        {
            var dispatcher = new Mock<IEventDispatcher>();
            dispatcher.SetupSequence(d => d.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("dispatch failed"))
                .Returns(Task.CompletedTask);

            var lifetime = new DomainEventDispatchLifetime(
                dispatcher.Object,
                NullLoggerFactory.Instance,
                Options.Create(new DomainEventOptions { OnEventFailure = DomainEventOptions.EventFailureStrategy.ThrowOnError }),
                new DomainEventDispatchTracker());

            var entity = new TestEntity();
            var domainEvent = new TestDomainEvent();
            entity.Raise(domainEvent);

            // A failed dispatch throws and must remain eligible for a later retry.
            await Assert.ThrowsAsync<DomainEventException>(() =>
                lifetime.PreSaveChangesAsync(RepositoryEntityStates.Added, entity, CancellationToken.None).AsTask());

            entity.Raise(domainEvent);
            await lifetime.PreSaveChangesAsync(RepositoryEntityStates.Added, entity, CancellationToken.None);

            dispatcher.Verify(d => d.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
