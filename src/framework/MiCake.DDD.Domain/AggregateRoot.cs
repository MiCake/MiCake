using System;

namespace MiCake.DDD.Domain
{
    public abstract class AggregateRoot : AggregateRoot<int>
    {
    }

    public abstract class AggregateRoot<TKey> :Entity<TKey>,IAggregateRoot<TKey>
    {
    }
}
