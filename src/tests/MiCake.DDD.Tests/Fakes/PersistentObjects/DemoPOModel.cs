using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Tests.Fakes.Aggregates;

namespace MiCake.DDD.Tests.Fakes.PersistentObjects
{
    public class DemoPOModel : PersistentObject<HasPOAggregateRoot>
    {
        public int No { get; set; }
        public string Name { get; set; }
        public int EntityNo { get; set; }

        public DemoPOModel()
        {
        }

        public override void ConfigureMapping()
        {
        }
    }
}
