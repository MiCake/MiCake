using MiCake.Core.Util;
using MiCake.Core.Util.Reflection;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.DDD.Extensions.Store.Configure
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// Provide core logic for completing the configuration of <see cref="StoreEntityType"/>
    /// </summary>
    public class InternalStoreEntityBuilder : IConventionStoreEntityBuilder
    {
        private Type _clrType;

        public StoreEntityType Metadata { get; private set; }

        public InternalStoreEntityBuilder(StoreEntityType storeEntity)
        {
            CheckValue.NotNull(storeEntity, nameof(storeEntity));

            Metadata = storeEntity;
            _clrType = storeEntity.ClrType;
        }

        public IConventionStoreEntityBuilder SetDirectDeletion(bool directDeletion)
        {
            Metadata.SetDirectDeletion(directDeletion);
            return this;
        }

        public IConventionStoreEntityBuilder AddIgnoredMember(string propertyName)
        {
            CheckValue.NotNullOrEmpty(propertyName, nameof(propertyName));

            if (_clrType.GetMembersInHierarchy(propertyName).Count() == 0)
            {
                throw new ArgumentException($"The property name '{propertyName}' is not belong to {_clrType.Name}");
            }
            Metadata.AddIgnoredMember(propertyName);

            return this;
        }

        public InternalStorePropertyBuilder AddProperty(string propertyName)
            => AddProperty(propertyName, null);

        public InternalStorePropertyBuilder AddProperty(string propertyName, MemberInfo memberInfo)
        {
            CheckValue.NotNullOrEmpty(propertyName, nameof(propertyName));

            var existedProperty = Metadata.FindProperty(propertyName);
            if (existedProperty != null)
            {
                return ((StoreProperty)existedProperty).Builder;
            }

            if (memberInfo == null)
            {
                var clrMember = Metadata.ClrType.GetMembersInHierarchy(propertyName).FirstOrDefault()
                                    ?? throw new InvalidOperationException($"The property '{propertyName}' cannot be added to the type '{Metadata.ClrType.Name}' " +
                                         $"because there was no property type specified and there is no corresponding CLR property or field.");

                memberInfo = clrMember;
            }

            return Metadata.AddProperty(propertyName, memberInfo).Builder;
        }

        public IConventionStoreEntityBuilder AddQueryFilter(LambdaExpression expression)
        {
            CheckValue.NotNull(expression, nameof(expression));
            Metadata.AddQueryFilter(expression);

            return this;
        }

        IConventionStorePropertyBuilder IConventionStoreEntityBuilder.AddProperty(string propertyName, MemberInfo memberInfo)
            => AddProperty(propertyName, memberInfo);
    }
}
