namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Used to activate mapping between aggregateRoot and persistent object.
    /// </summary>
    public interface IPersistentObjectActivator
    {
        void ActivateMapping();
    }
}
