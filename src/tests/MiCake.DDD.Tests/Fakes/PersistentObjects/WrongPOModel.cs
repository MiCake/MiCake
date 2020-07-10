using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Tests.Fakes.Aggregates;

namespace MiCake.DDD.Tests.Fakes.PersistentObjects
{
    public class WrongPOModel : PersistentObject<int, HasEventsAggregate, WrongPOModel>
    {
        public override void ConfigureMapping()
        {
            //  throw new NotImplementedException();
        }
    }
}
