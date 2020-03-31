using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Tests.Fakes.Aggregates;

namespace MiCake.DDD.Tests.Fakes.StorageModels
{
    public class WrongPOModel : StorageModel<HasEventsAggregate>
    {
        public override void ConfigureMapping()
        {
            //  throw new NotImplementedException();
        }
    }
}
