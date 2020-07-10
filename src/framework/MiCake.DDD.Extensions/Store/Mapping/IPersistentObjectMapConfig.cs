using MiCake.DDD.Domain;
using System;
using System.Linq.Expressions;

namespace MiCake.DDD.Extensions.Store.Mapping
{
    /// <summary>
    /// Indicates the configuration rules for mapping the current persistent object to the domain object.
    /// </summary>
    public interface IPersistentObjectMapConfig
    {
        /// <summary>
        /// Build current config rules.
        /// </summary>
        void Build();
    }

    /// <summary>
    /// Indicates the configuration rules for mapping the current persistent object to the domain object.
    /// </summary>
    /// <typeparam name="TKey">unique key type of domain entity and persistent object</typeparam>
    /// <typeparam name="TDomainEntity">The type of domain entity</typeparam>
    /// <typeparam name="TPersistentObject">The type of persistent object</typeparam>
    public interface IPersistentObjectMapConfig<TKey, TDomainEntity, TPersistentObject> : IPersistentObjectMapConfig
        where TDomainEntity : IAggregateRoot<TKey>
        where TPersistentObject : IPersistentObject<TKey, TDomainEntity>
    {
        /// <summary>
        /// Indicates the correspondence between a property of an entity and a property of a persistent object.
        /// </summary>
        /// <typeparam name="TEntityProperty">The type of domain entity</typeparam>
        /// <typeparam name="TPersistentObjectProperty">The type of persistent object</typeparam>
        /// <param name="domainEntiyProperty">The Func to choose domain entity property</param>
        /// <param name="persistentObjectProperty">The Func to choose persistent object property</param>
        /// <returns></returns>
        IPersistentObjectMapConfig<TKey, TDomainEntity, TPersistentObject> MapProperty<TEntityProperty, TPersistentObjectProperty>(
                Expression<Func<TDomainEntity, TEntityProperty>> domainEntiyProperty,
                Expression<Func<TPersistentObject, TPersistentObjectProperty>> persistentObjectProperty);
    }
}
