namespace MiCake.DDD.Domain
{
    /// <summary>
    /// Base class for aggregate roots with integer identity.
    /// </summary>
    public abstract class AggregateRoot : AggregateRoot<int>
    {
    }

    /// <summary>
    /// Base class for aggregate roots.
    /// Aggregate roots are the root entities of aggregates in Domain-Driven Design.
    /// </summary>
    /// <typeparam name="TKey">The type of the aggregate root identifier</typeparam>
    public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey> where TKey : notnull
    {
    }
}
