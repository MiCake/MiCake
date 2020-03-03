using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using System;


namespace MiCake.DDD.Tests.Fakes.StorageModels
{
    public class HasStorageModelAggregateRoot : StorageModelAggregateRoot<Guid>
    {
        public int No { get; private set; }
        public string Name { get; private set; }

        public DemoEntity EntityInfo { get; private set; }
    }

    public class DemoEntity : Entity<Guid>
    {
        public int EntityNo { get; set; }
    }
}
