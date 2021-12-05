using MiCake.Core.Util;
using System;
using System.Reflection;

namespace MiCake.DDD.Connector.Store.Configure
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// Represents a description of an object property that needs to be persisted.
    /// </summary>
    public class StoreProperty : IStoreProperty, IConventionStoreProperty
    {
        private readonly Type _clrPropertyType;
        private readonly PropertyInfo _clrPropertyInfo;
        private readonly MemberInfo _clrMemberInfo;
        private readonly FieldInfo _clrFieldInfo;
        private readonly IStoreEntityType _originalEntity;

        private bool? _isConcurrency;
        private bool? _isNullable;
        private int? _maxLength;
        private object? _defaultValue;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public PropertyInfo ClrPropertyInfo => _clrPropertyInfo;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IStoreEntityType StoreEntityType => _originalEntity;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool? IsConcurrency => _isConcurrency;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool? IsNullable => _isNullable;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int? MaxLength => _maxLength;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public object? DefaultValue => _defaultValue;

        public string Name { get; }

        public InternalStorePropertyBuilder Builder { get; private set; }

        public FieldInfo ClrFieldInfo => _clrFieldInfo;

        public StoreProperty(string name,
                             Type propertyType,
                             PropertyInfo propertyInfo,
                             FieldInfo fieldInfo,
                             MemberInfo memberInfo,
                             StoreEntityType storeEntityType)
        {
            CheckValue.NotNullOrEmpty(name, nameof(name));
            CheckValue.NotNull(propertyType, nameof(propertyType));
            CheckValue.NotNull(storeEntityType, nameof(storeEntityType));

            Name = name;
            _clrPropertyType = propertyType;
            _clrPropertyInfo = propertyInfo;
            _clrFieldInfo = fieldInfo;
            _clrMemberInfo = memberInfo;
            _originalEntity = storeEntityType;

            Builder = new InternalStorePropertyBuilder(this);
        }

        public void SetConcurrency(bool isConcurrencyPropery)
            => _isConcurrency = isConcurrencyPropery;

        public void SetDefaultValue(object value)
            => _defaultValue = value;

        public void SetMaxLength(int maxlength)
            => _maxLength = maxlength;

        public void SetNullable(bool isNullable)
            => _isNullable = isNullable;
    }
}
