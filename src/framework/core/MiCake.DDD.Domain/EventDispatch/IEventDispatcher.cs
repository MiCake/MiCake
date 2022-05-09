namespace MiCake.DDD.Domain.EventDispatch
{
    public interface IEventDispatcher
    {
        Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default) where TDomainEvent : IDomainEvent;
    }
}
