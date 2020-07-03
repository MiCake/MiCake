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
        /// Add the mapping relationship between persistent objects and domain objects.
        /// </summary>
        /// <param name="domainEntity">Domain object.</param>
        /// <param name="persistentObj">Persistent object.</param>
        void Add(object domainEntity, object persistentObj);

        /// <summary>
        /// Get the persistent object according to the current domain object.
        /// <para>
        ///     Values can be collections, objects, etc. Because only need its hash value.
        /// </para>
        /// </summary>
        /// <param name="domainEntity">Domain object.</param>
        /// <param name="persistentObj">Persistent object.</param>
        /// <returns>if has value return true.</returns>
        bool TryGetPersistentObj(object domainEntity, out object persistentObj);

        /// <summary>
        /// Get the domain object according to the current persistent object.
        /// <para>
        ///     Values can be collections, objects, etc. Because only need its hash value.
        /// </para>
        /// </summary>
        /// <param name="persistentObj">Persistent object.</param>
        /// <param name="domainEntity">Domain object.</param>
        /// <returns>if has value return true.</returns>
        bool TryGetDomainEntity(object persistentObj, out object domainEntity);
    }
}
