using MiCake.Core.Data;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Defines the relationship between persistent objects and domain objects
    /// 
    /// <para>
    ///     When using persistent objects, may need to track the relationship between them. 
    ///     For example, in efcore, we need to use domain objects in business, and use persistent objects when saving. 
    ///     However, entities are tracked by efcore. We need to synchronize to the original persistent objects after the domain object value changes.
    /// </para>
    /// </summary>
    public interface IPersistentObjectReferenceMap : IReleasableService
    {
        /// <summary>
        /// Add the mapping relationship between persistent objects and domain objects,Update if it already exists.
        /// </summary>
        /// <param name="domainEntity">Domain object.</param>
        /// <param name="persistentObj">Persistent object.</param>
        void AddOrUpdate(object domainEntity, object persistentObj);

        /// <summary>
        /// Get the persistent object according to the current domain object
        /// </summary>
        /// <param name="domainEntity">Domain object.</param>
        /// <param name="persistentObj">Persistent object.</param>
        /// <returns>if has value return true.</returns>
        bool TryGet(object domainEntity, out object persistentObj);
    }
}
