namespace MiCake.DDD.Domain.Store
{
    /// <summary>
    /// A aggregate root who Has persistent object
    /// </summary>
    /// <typeparam name="TKey">Primary of aggregateroot</typeparam>
    public abstract class AggregateRootHasPersistentObject<TKey> :
        AggregateRoot<TKey>,
        IHasPersistentObject
    {
        public AggregateRootHasPersistentObject()
        {
        }
    }
}
