using MiCake.Core.Util;
using System;

namespace MiCake.DDD.Extensions.Store.Configure
{
    public class InternalStorePropertyBuilder : IConventionStorePropertyBuilder
    {
        public StoreProperty Metadata { get; }

        public InternalStorePropertyBuilder(StoreProperty storeProperty)
        {
            CheckValue.NotNull(storeProperty, nameof(storeProperty));

            Metadata = storeProperty;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConventionStorePropertyBuilder SetConcurrency(bool isConcurrencyPropery)
        {
            Metadata.SetConcurrency(isConcurrencyPropery);
            return this;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConventionStorePropertyBuilder SetNullable(bool isNullable)
        {
            Metadata.SetNullable(isNullable);
            return this;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConventionStorePropertyBuilder SetMaxLength(int maxlength)
        {
            if (maxlength < 0)
                throw new ArgumentException($"{nameof(maxlength)} cannot less than 0.");

            Metadata.SetMaxLength(maxlength);
            return this;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConventionStorePropertyBuilder SetDefaultValue(object value)
        {
            Metadata.SetDefaultValue(value);
            return this;
        }
    }
}
