using MiCake.DDD.Domain;

namespace MiCake.DDD.Extensions.Store.Mapping
{
    /// <summary>
    /// Indicates the mapper that maps persistent objects to domain objects.
    /// </summary>
    public interface IPersistentObjectMapper
    {
        /// <summary>
        /// Whether to initialize automatically when the MiCake module startup.
        /// </summary>
        bool InitAtStartup { get; }

        /// <summary>
        /// Create mapping relationship between domain entity and persistent object.
        /// 
        /// <para>
        ///     This method return <see cref="IPersistentObjectMapConfig{TKey,TDomainEntity, TPersistentObject}"/>,you will use it to config mapping rule.
        /// </para>
        /// </summary>
        /// <typeparam name="TKey">unique key type</typeparam>
        /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
        /// <returns><see cref="IPersistentObjectMapConfig{TKey,TDomainEntity, TPersistentObject}"/></returns>
        IPersistentObjectMapConfig<TKey, TDomainEntity, TPersistentObject> Create<TKey, TDomainEntity, TPersistentObject>()
            where TDomainEntity : IAggregateRoot<TKey>
            where TPersistentObject : IPersistentObject<TKey, TDomainEntity>;

        /// <summary>
        /// Map domain entity to persistent object.
        /// </summary>
        /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
        /// <param name="domainEntity"></param>
        /// <returns></returns>
        TPersistentObject ToPersistentObject<TDomainEntity, TPersistentObject>(TDomainEntity domainEntity)
            where TDomainEntity : IAggregateRoot
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
            where TDomainEntity : IAggregateRoot
            where TPersistentObject : IPersistentObject;

        /// <summary>
        /// Map persistent object to domain entity.
        /// </summary>
        /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
        /// <param name="persistentObject"></param>
        /// <returns></returns>
        TDomainEntity ToDomainEntity<TDomainEntity, TPersistentObject>(TPersistentObject persistentObject)
            where TDomainEntity : IAggregateRoot
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
            where TDomainEntity : IAggregateRoot
            where TPersistentObject : IPersistentObject;

        /// <summary>
        /// Mapping entity and persistent object Freely.
        /// Can mapping entity list to persistent list or other.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">Need mapping source value.</param>
        /// <returns></returns>
        TDestination Map<TSource, TDestination>(TSource source);

        /// <summary>
        /// Mapping entity and persistent object Freely based on the destination original value.
        /// Can mapping entity list to persistent list or other.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">Need mapping source value.</param>
        /// <param name="originalValue">original destination value.</param>
        /// <returns></returns>
        TDestination Map<TSource, TDestination>(TSource source, TDestination originalValue);
    }
}
