using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain
{
    public class AggregateRoot : AggregateRoot<int>
    {
    }

    public class AggregateRoot<TKey> : IAggregateRoot<TKey>
    {
        public TKey Id { get; set; }

        public AggregateRoot()
        {
        }
    }
}
