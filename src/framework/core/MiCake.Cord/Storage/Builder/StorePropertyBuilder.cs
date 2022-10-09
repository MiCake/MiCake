using MiCake.Cord.Storage.Internal;
using MiCake.Core.Data;
using MiCake.Core.Util;

namespace MiCake.Cord.Storage.Builder
{
    public class StorePropertyBuilder : IHasAccessor<StoreProperty>
    {
        protected readonly StoreProperty _propertySource;

        StoreProperty IHasAccessor<StoreProperty>.AccessibleData => _propertySource;

        public StorePropertyBuilder(IStoreProperty storeProperty)
        {
            CheckValue.NotNull(storeProperty, nameof(storeProperty));

            _propertySource = (StoreProperty)storeProperty;
        }

        /// <summary>
        /// Indicates whether the property will be used as a concurrent judgment
        /// </summary>
        public StorePropertyBuilder Concurrency(bool isConcurrencyPropery)
        {
            _propertySource.SetConcurrency(isConcurrencyPropery);
            return this;
        }

        /// <summary>
        /// Indicates whether null is allowed for this property
        /// </summary>
        public StorePropertyBuilder Nullable(bool isNullable)
        {
            _propertySource.SetNullable(isNullable);
            return this;
        }

        /// <summary>
        /// Set the maximum length of this property.
        /// If value is null,max length is default.
        /// </summary>
        public StorePropertyBuilder MaxLength(int maxlength)
        {
            _propertySource.SetMaxLength(maxlength);
            return this;
        }

        /// <summary>
        /// Set the default value for this property
        /// </summary>
        public StorePropertyBuilder DefaultValue(object value, StorePropertyDefaultValueType valueType = StorePropertyDefaultValueType.ClrValue, StorePropertyDefaultValueSetOpportunity setOpportunity = StorePropertyDefaultValueSetOpportunity.AddAndUpdate)
        {
            _propertySource.SetDefaultValue(value, valueType, setOpportunity);
            return this;
        }
    }
}
