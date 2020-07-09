using AutoMapper;
using AutoMapper.Configuration;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Mapping;
using System;
using System.Linq.Expressions;

namespace MiCake.AutoMapper
{
    /// <summary>
    /// <see cref="IPersistentObjectMapConfig{TDomainEntity, TPersistentObject}"/> implement for AutoMapper.
    /// </summary>
    internal class AutoMapperPersistentObjectMapConfig<TDomainEntity, TPersistentObject> : MappingExpression<TDomainEntity, TPersistentObject>, IPersistentObjectMapConfig<TDomainEntity, TPersistentObject>
          where TDomainEntity : IEntity
          where TPersistentObject : IPersistentObject
    {
        public AutoMapperPersistentObjectMapConfig(MemberList memberList) : base(memberList)
        {
        }

        public AutoMapperPersistentObjectMapConfig(MemberList memberList, TypePair types) : base(memberList, types)
        {
        }

        public AutoMapperPersistentObjectMapConfig(MemberList memberList, Type sourceType, Type destinationType) : base(memberList, sourceType, destinationType)
        {
        }

        public IPersistentObjectMapConfig<TDomainEntity, TPersistentObject> MapProperty<TEntityProperty, TPersistentObjectProperty>(
            Expression<Func<TDomainEntity, TEntityProperty>> domainEntiyProperty,
            Expression<Func<TPersistentObject, TPersistentObjectProperty>> persistentObjectProperty)
        {
            ForMember(persistentObjectProperty, opt => opt.MapFrom(domainEntiyProperty));
            return this;
        }
    }
}
