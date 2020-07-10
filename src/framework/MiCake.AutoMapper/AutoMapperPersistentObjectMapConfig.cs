using AutoMapper;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;
using System;
using System.Linq.Expressions;

namespace MiCake.AutoMapper
{
    /// <summary>
    /// <see cref="IPersistentObjectMapConfig{TKey,TDomainEntity, TPersistentObject}"/> implement for AutoMapper.
    /// </summary>
    internal class AutoMapperPersistentObjectMapConfig<TKey, TDomainEntity, TPersistentObject> : IPersistentObjectMapConfig<TKey, TDomainEntity, TPersistentObject>
          where TDomainEntity : IAggregateRoot<TKey>
          where TPersistentObject : IPersistentObject<TKey, TDomainEntity>
    {
        private IMappingExpression<TDomainEntity, TPersistentObject> _autoMapperExpression;
        public AutoMapperPersistentObjectMapConfig(IMappingExpression<TDomainEntity, TPersistentObject> autoMapperExpression)
            => _autoMapperExpression = autoMapperExpression;

        public void Build()
        {
            _autoMapperExpression
                .AfterMap((s, d) => d.AddDomainEvents(s.GetDomainEvents()))     //Give DomainEvents to persistent object.
                .ReverseMap();
        }

        public IPersistentObjectMapConfig<TKey, TDomainEntity, TPersistentObject> MapProperty<TEntityProperty, TPersistentObjectProperty>(
            Expression<Func<TDomainEntity, TEntityProperty>> domainEntiyProperty,
            Expression<Func<TPersistentObject, TPersistentObjectProperty>> persistentObjectProperty)
        {
            _autoMapperExpression.ForMember(persistentObjectProperty, opt => opt.MapFrom(domainEntiyProperty));
            return this;
        }
    }
}
