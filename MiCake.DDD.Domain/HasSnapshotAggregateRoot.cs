using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain
{
    /// <summary>
    /// a has snapshot aggregateRoot
    /// </summary>
    /// <typeparam name="TKey">primary of aggregateroot</typeparam>
    /// <typeparam name="TSnapshot">snapshot of aggregateroot</typeparam>
    public abstract class HasSnapshotAggregateRoot<TKey, TSnapshot> :
        IEntityHasSnapshot<TSnapshot>,
        IAggregateRoot<TKey>
    {
        public TKey Id { get; set; }

        public HasSnapshotAggregateRoot()
        {
        }

        public HasSnapshotAggregateRoot(TSnapshot snapshot)
        {
        }

        public abstract TSnapshot GetSnapshot();
    }
}
