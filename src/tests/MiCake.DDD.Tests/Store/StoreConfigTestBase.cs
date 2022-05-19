using MiCake.Cord.Storage;

namespace MiCake.Cord.Tests.Store
{
    public abstract class StoreConfigTestBase
    {
        protected IStoreModel CreateModel() => new StoreModel();

        protected StoreModelBuilder CreateStoreModelBuilder() => new(CreateModel());
    }
}
