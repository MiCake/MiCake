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
    }
}
