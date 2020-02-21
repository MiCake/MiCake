namespace MiCake.DDD.Domain.Store
{
    /// <summary>
    /// Has storage model aggregateRoot
    /// </summary>
    /// <typeparam name="TKey">Primary of aggregateroot</typeparam>
    /// <typeparam name="TStorageModel"><see cref="IStorageModel"/></typeparam>
    public abstract class StorageModelAggregateRoot<TKey> :
        AggregateRoot<TKey>,
        IHasStorageModel
    {
        public StorageModelAggregateRoot()
        {
        }
    }
}
