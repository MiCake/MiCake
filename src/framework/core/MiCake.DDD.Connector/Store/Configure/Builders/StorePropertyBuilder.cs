using MiCake.Core.Data;
using MiCake.Core.Util;

namespace MiCake.DDD.Connector.Store.Configure
{
    public class StorePropertyBuilder : IHasAccessor<InternalStorePropertyBuilder>
    {
        InternalStorePropertyBuilder IHasAccessor<InternalStorePropertyBuilder>.Instance => _builer;

        private readonly InternalStorePropertyBuilder _builer;

        public StorePropertyBuilder(IStoreProperty storeProperty)
        {
            CheckValue.NotNull(storeProperty, nameof(storeProperty));

            _builer = ((StoreProperty)storeProperty).Builder;
        }

        /// <summary>
        /// Indicates whether the property will be used as a concurrent judgment
        /// </summary>
        public StorePropertyBuilder Concurrency(bool isConcurrencyPropery)
        {
            _builer.SetConcurrency(isConcurrencyPropery);
            return this;
        }

        /// <summary>
        /// Indicates whether null is allowed for this property
        /// </summary>
        public StorePropertyBuilder Nullable(bool isNullable)
        {
            _builer.SetNullable(isNullable);
            return this;
        }

        /// <summary>
        /// Set the maximum length of this property.
        /// If value is null,max length is default.
        /// </summary>
        public StorePropertyBuilder MaxLength(int maxlength)
        {
            _builer.SetMaxLength(maxlength);
            return this;
        }

        /// <summary>
        /// Set the default value for this property
        /// </summary>
        public StorePropertyBuilder DefaultValue(object value)
        {
            _builer.SetDefaultValue(value);
            return this;
        }
    }
}
