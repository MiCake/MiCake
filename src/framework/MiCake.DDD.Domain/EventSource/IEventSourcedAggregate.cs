using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Domain.EventSource
{
    /// <summary>
    /// A aggregate root use event source
    /// </summary>
    public interface IEventSourcedAggregate<TKey>:IAggregateRoot<TKey>
    {

    }
}
