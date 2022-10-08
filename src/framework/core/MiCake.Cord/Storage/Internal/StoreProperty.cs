using MiCake.Core.Util;
using System.Reflection;

namespace MiCake.Cord.Storage.Internal
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
        private readonly IStoreEntityType _originalEntity;

        private bool? _isConcurrency;
        private bool? _isNullable;
        private int? _maxLength;
        private StorePropertyDefaultValue? _defaultValue;

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
        public StorePropertyDefaultValue? DefaultValue => _defaultValue;

        public string Name { get; }

        public StoreProperty(string name,
                             Type propertyType,
                             PropertyInfo propertyInfo,
                             StoreEntityType storeEntityType)
        {
            CheckValue.NotNullOrEmpty(name, nameof(name));
            CheckValue.NotNull(propertyType, nameof(propertyType));
            CheckValue.NotNull(storeEntityType, nameof(storeEntityType));

            Name = name;
            _clrPropertyType = propertyType;
            _clrPropertyInfo = propertyInfo;
            _originalEntity = storeEntityType;
        }

        public void SetConcurrency(bool isConcurrencyPropery) => _isConcurrency = isConcurrencyPropery;

        public void SetDefaultValue(object value, StorePropertyDefaultValueType valueType = StorePropertyDefaultValueType.ClrValue, StorePropertyDefaultValueSetOpportunity setOpportunity = StorePropertyDefaultValueSetOpportunity.AddAndUpdate)
            => _defaultValue = new StorePropertyDefaultValue(value, valueType, setOpportunity);

        public void SetMaxLength(int maxlength)
        {
            if (maxlength < 0)
                throw new ArgumentException($"{nameof(maxlength)} cannot less than 0.");

            _maxLength = maxlength;
        }

        public void SetNullable(bool isNullable) => _isNullable = isNullable;
    }
}
