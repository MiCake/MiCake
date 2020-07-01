using MiCake.DDD.Domain;
using System;

namespace MiCake.DDD.Extensions.Store.Mapping
{
    /// <summary>
    /// Indicates the configuration rules for mapping the current persistent object to the domain object.
    /// </summary>
    /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
    /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
    public interface IPersistentObjectMapConfig<TDomainEntity, TPersistentObject>
        where TDomainEntity : IEntity
        where TPersistentObject : IPersistentObject
    {
        /// <summary>
        /// Indicates the correspondence between a property of an entity and a property of a persistent object.
        /// </summary>
        /// <typeparam name="TEntityProperty">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObjectProperty">The type of persistent object</typeparam>
        /// <param name="domainEntiyProperty">The Func to choose domain entity property</param>
        /// <param name="persistentObjectProperty">The Func to choose persistent object property</param>
        /// <returns></returns>
        IPersistentObjectMapConfig<TDomainEntity, TPersistentObject> MapProperty<TEntityProperty, TPersistentObjectProperty>(
                Func<TDomainEntity, TEntityProperty> domainEntiyProperty,
                Func<TDomainEntity, TPersistentObjectProperty> persistentObjectProperty);
    }
}
