namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Handling of domain events
    /// </summary>
    /// <typeparam name="TDomainEvent"><see cref="IDomainEvent"/></typeparam>
    public interface IDomainEventHandler<in TDomainEvent>
        where TDomainEvent : IDomainEvent
    {
        Task HandleAysnc(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
