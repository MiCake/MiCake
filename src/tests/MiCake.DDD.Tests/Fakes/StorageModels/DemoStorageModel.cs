using MiCake.DDD.Extensions.Store;

namespace MiCake.DDD.Tests.Fakes.StorageModels
{
    public class DemoStorageModel : StorageModel<DemoAggregateRoot>
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
