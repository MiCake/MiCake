namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Used to activate mapping between aggregateRoot and storagemodel
    /// </summary>
    public interface IStorageModelActivator
    {
        void ActivateMapping();
    }
}
