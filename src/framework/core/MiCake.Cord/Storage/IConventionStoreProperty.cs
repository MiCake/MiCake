namespace MiCake.Cord.Storage
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// 
    /// Expose interfaces that store property can configure
    /// </summary>
    public interface IConventionStoreProperty
    {
        /// <summary>
        /// Indicates whether the property will be used as a concurrent judgment
        /// </summary>
        bool? IsConcurrency { get; }

        /// <summary>
        /// Indicates whether null is allowed for this property
        /// </summary>
        bool? IsNullable { get; }

        /// <summary>
        /// Max length for this property
        /// </summary>
        int? MaxLength { get; }

        /// <summary>
        /// Default value for this property
        /// </summary>
        StorePropertyDefaultValue? DefaultValue { get; }

        /// <summary>
        /// Indicates whether the property will be used as a concurrent judgment
        /// </summary>
        void SetConcurrency(bool isConcurrencyPropery);

        /// <summary>
        /// Indicates whether null is allowed for this property
        /// </summary>
        void SetNullable(bool isNullable);

        /// <summary>
        /// Set the maximum length of this property.
        /// If value is null,max length is default.
        /// </summary>
        void SetMaxLength(int maxlength);

        /// <summary>
        /// Set the default value for this property
        /// </summary>
        void SetDefaultValue(object value, StorePropertyDefaultValueType valueType = StorePropertyDefaultValueType.ClrValue, StorePropertyDefaultValueSetOpportunity setOpportunity = StorePropertyDefaultValueSetOpportunity.AddAndUpdate);
    }
}
