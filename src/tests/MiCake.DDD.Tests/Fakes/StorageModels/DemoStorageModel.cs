using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Tests.Fakes.Aggregates;

namespace MiCake.DDD.Tests.Fakes.StorageModels
{
    public class DemoStorageModel : StorageModel<HasStorageModelAggregateRoot>
    {
        public int No { get; set; }
        public string Name { get; set; }
        public int EntityNo { get; set; }

        public DemoStorageModel()
        {
        }

        public override void ConfigureMapping()
        {
        }
    }
}
