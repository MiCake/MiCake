using System;

namespace MiCake.DDD.Domain
{
    public abstract class AggregateRoot : AggregateRoot<int>
    {
    }

    [Serializable]
    public abstract class AggregateRoot<TKey> : IAggregateRoot<TKey>
    {
        public TKey Id { get; set; }

        public AggregateRoot()
        {
        }
    }
}
