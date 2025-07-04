﻿using MiCake.Core.Util;
using MiCake.Core.Util.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.DDD.Extensions.Store.Configure
{
    /// <summary>
    /// This is an internal API not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// Represents a description of an object that needs to be persisted.
    /// </summary>
    public class StoreEntityType : IStoreEntityType, IConventionStoreEntity
    {
        private readonly Type _originalType;
        private bool _directDeletion = true;

        private readonly Dictionary<string, StoreProperty> _properties;
        private readonly List<LambdaExpression> _filterExpressions;
        private readonly List<string> _ignoredMembers;

        /// <summary>
        /// Indicates whether the persistent object needs to be removed directly from the database
        /// </summary>
        public virtual bool DirectDeletion => _directDeletion;
        public virtual Type ClrType => _originalType;
        public virtual InternalStoreEntityBuilder Builder { get; }
        public virtual string Name { get; }

        public StoreEntityType(Type clrType)
        {
            CheckValue.NotNull(clrType, nameof(clrType));

            _originalType = clrType;
            Name = clrType.Name;

            _properties = [];
            _ignoredMembers = [];
            _filterExpressions = [];

            Builder = new InternalStoreEntityBuilder(this);
        }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public virtual StoreProperty AddProperty(string name)
        {
            CheckValue.NotNull(name, nameof(name));

            var clrMember = _originalType.GetMembersInHierarchy(name).FirstOrDefault();
            if (clrMember == null)
            {
                throw new InvalidOperationException($"The property '{name}' cannot be added to the type '{_originalType.Name}' " +
                    $"because there was no property type specified and there is no corresponding CLR property or field.");
            }

            return AddProperty(name, clrMember);
        }

        /// <summary>
        /// Add the property information required for the persistence object
        /// </summary>
        public virtual StoreProperty AddProperty(string name, MemberInfo memberInfo)
        {
            CheckValue.NotNull(name, nameof(name));
            CheckValue.NotNull(memberInfo, nameof(memberInfo));

            // Currently, only property configuration is supported. 
            // If memberinfo is a field type, an error will also be prompted
            if (memberInfo.MemberType != MemberTypes.Property)
            {
                throw new InvalidOperationException($"The {nameof(AddProperty)} method only property is supported," +
                    $" but the parameter '{nameof(memberInfo)}' type is {memberInfo.MemberType}.");
            }

            var propertyType = memberInfo.GetMemberType();
            var property = new StoreProperty(name,
                                             propertyType,
                                             memberInfo as PropertyInfo,
                                             memberInfo as FieldInfo,
                                             memberInfo,
                                             this);
            _properties.Add(name, property);

            return property;
        }

        /// <summary>
        /// Get the property configuration of the persistent object
        /// </summary>
        public virtual IEnumerable<IStoreProperty> GetProperties()
            => _properties.Values.Cast<IStoreProperty>();

        public virtual IStoreProperty FindProperty(string name)
            => _properties.TryGetValue(CheckValue.NotEmpty(name, nameof(name)), out var value) ? value : null;

        /// <summary>
        /// Mark whether the persistent object needs to be removed directly from the database
        /// If do not need to delete directly, the database provider may use soft deletion to process
        /// </summary>
        public virtual void SetDirectDeletion(bool directDeletion)
            => _directDeletion = directDeletion;

        /// <summary>
        /// Get the ignored properties configuration of the persistent object
        /// </summary>
        public virtual IEnumerable<string> GetIgnoredMembers()
            => _ignoredMembers;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void AddIgnoredMember(string propertyName)
        {
            CheckValue.NotNullOrEmpty(propertyName, nameof(propertyName));

            if (_ignoredMembers.Contains(propertyName))
                return;

            _ignoredMembers.Add(propertyName);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void AddQueryFilter(LambdaExpression expression)
        {
            CheckValue.NotNull(expression, nameof(expression));

            _filterExpressions.Add(expression);
        }

        public virtual IEnumerable<LambdaExpression> GetQueryFilters()
            => _filterExpressions;

        IStoreProperty IConventionStoreEntity.AddProperty(string name, MemberInfo memberInfo)
            => AddProperty(name, memberInfo);

        IStoreProperty IConventionStoreEntity.FindProperty(string name)
            => FindProperty(name);

        IEnumerable<IStoreProperty> IConventionStoreEntity.GetProperties()
            => GetProperties();

        void IConventionStoreEntity.SetDirectDeletion(bool directDeletion)
            => SetDirectDeletion(directDeletion);

        void IConventionStoreEntity.AddIgnoredMember(string propertyName)
            => AddIgnoredMember(propertyName);

        IEnumerable<string> IConventionStoreEntity.GetIgnoredMembers()
            => GetIgnoredMembers();

        void IConventionStoreEntity.AddQueryFilter(LambdaExpression expression)
            => AddQueryFilter(expression);

        IEnumerable<LambdaExpression> IConventionStoreEntity.GetQueryFilters()
            => GetQueryFilters();
    }
}
