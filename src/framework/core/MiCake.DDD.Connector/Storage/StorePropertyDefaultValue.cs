namespace MiCake.Cord.Storage
{
    public struct StorePropertyDefaultValue
    {
        public object? DefaultValue { get; }

        public StorePropertyDefaultValueSetOpportunity SetOpportunity { get; }

        public StorePropertyDefaultValueType ValueType { get; }

        public StorePropertyDefaultValue(object value, StorePropertyDefaultValueType valueType, StorePropertyDefaultValueSetOpportunity setOpportunity)
        {
            DefaultValue = value;
            SetOpportunity = setOpportunity;
            ValueType = valueType;
        }
    }

    public enum StorePropertyDefaultValueType
    {
        ClrValue = 1,

        SqlValue = 2,
    }

    public enum StorePropertyDefaultValueSetOpportunity
    {
        Add = 1,

        Update = 2,

        AddAndUpdate = 4
    }
}
