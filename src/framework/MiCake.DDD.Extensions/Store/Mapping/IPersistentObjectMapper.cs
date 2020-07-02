using MiCake.DDD.Domain;

namespace MiCake.DDD.Extensions.Store.Mapping
{
    /// <summary>
    /// Indicates the mapper that maps persistent objects to domain objects.
    /// </summary>
    public interface IPersistentObjectMapper
    {
        /// <summary>
        /// Create mapping relationship between domain entity and persistent object.
        /// 
        /// <para>
        ///     This method return <see cref="IPersistentObjectMapConfig{TDomainEntity, TPersistentObject}"/>,you will use it to config mapping rule.
        /// </para>
        /// </summary>
        /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
        /// <returns><see cref="IPersistentObjectMapConfig{TDomainEntity, TPersistentObject}"/></returns>
        IPersistentObjectMapConfig<TDomainEntity, TPersistentObject> Create<TDomainEntity, TPersistentObject>()
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject;

        /// <summary>
        /// Map domain entity to persistent object.
        /// </summary>
        /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
        /// <param name="domainEntity"></param>
        /// <returns></returns>
        TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject;

        /// <summary>
        /// Map domain entity to persistent object.Mapping based on the original value.
        /// </summary>
        /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
        /// <param name="domainEntity"></param>
        /// <param name="originalSource">original persistent object value</param>
        /// <returns></returns>
        TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity, TPersistentObject originalSource)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject;

        /// <summary>
        /// Map persistent object to domain entity.
        /// </summary>
        /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
        /// <param name="persistentObject"></param>
        /// <returns></returns>
        TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject;

        /// <summary>
        /// Map persistent object to domain entity.Mapping based on the original value.
        /// </summary>
        /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
        /// <param name="persistentObject"></param>
        /// <param name="originalSource">original domain entity value</param>
        /// <returns></returns>
        TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject, TDomainEntity originalSource)
            where TDomainEntity : IEntity
            where TPersistentObject : IPersistentObject;
    }
}
