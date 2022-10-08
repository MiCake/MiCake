namespace MiCake.Cord.Storage
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// Represents the configuration information of an object that needs to be persisted
    /// By explaining the configuration, the external program persistence provider can complete the model configuration of persistent objects
    /// </summary>
    public interface IStoreModelProvider
    {
        void Config(StoreModelBuilder modelBuilder);
    }
}
