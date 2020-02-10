namespace MiCake.DDD.Domain
{
    /// <summary>
    /// a has snapshot aggregateRoot
    /// </summary>
    /// <typeparam name="TKey">primary of aggregateroot</typeparam>
    /// <typeparam name="TSnapshot">snapshot of aggregateroot</typeparam>
    public abstract class HasSnapshotAggregateRoot<TKey, TSnapshot> :
        AggregateRoot<TKey>,
        IEntityHasSnapshot<TSnapshot>
    {
        public HasSnapshotAggregateRoot()
        {
        }

        public HasSnapshotAggregateRoot(TSnapshot snapshot)
        {
        }

        public abstract TSnapshot GetSnapshot();
    }
}
