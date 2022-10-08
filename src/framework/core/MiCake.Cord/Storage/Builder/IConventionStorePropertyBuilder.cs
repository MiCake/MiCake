using MiCake.Cord.Storage.Internal;

namespace MiCake.Cord.Storage.Builder
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    ///  Provides a simple API surface for configuring an <see cref="StoreProperty" /> from conventions.
    ///  </summary>
    public interface IConventionStorePropertyBuilder
    {
        /// <summary>
        /// Indicates whether the property will be used as a concurrent judgment
        /// </summary>
        IConventionStorePropertyBuilder SetConcurrency(bool isConcurrencyPropery);

        /// <summary>
        /// Indicates whether null is allowed for this property
        /// </summary>
        IConventionStorePropertyBuilder SetNullable(bool isNullable);

        /// <summary>
        /// Set the maximum length of this property.
        /// If value is null,max length is default.
        /// </summary>
        IConventionStorePropertyBuilder SetMaxLength(int maxlength);

        /// <summary>
        /// Set the default value for this property
        /// </summary>
        IConventionStorePropertyBuilder SetDefaultValue(object value);
    }
}
