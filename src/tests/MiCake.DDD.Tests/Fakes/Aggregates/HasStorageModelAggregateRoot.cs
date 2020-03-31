using MiCake.DDD.Domain.Store;
using System;


namespace MiCake.DDD.Tests.Fakes.Aggregates
{
    public class HasStorageModelAggregateRoot : StorageModelAggregateRoot<Guid>
    {
        public int No { get; private set; }
        public string Name { get; private set; }
    }
}
