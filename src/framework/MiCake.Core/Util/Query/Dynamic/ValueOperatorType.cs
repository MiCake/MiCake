namespace MiCake.Util.Query.Dynamic
{
    /// <summary>
    /// Comparison operators used to check a value against a property (Equal, In, Contains, ...).
    /// </summary>
    public enum ValueOperatorType
    {
        /// <summary>
        /// "="
        /// </summary>
        Equal,

        /// <summary>
        /// "!="
        /// </summary>
        NotEqual,

        /// <summary>
        /// "&lt;"
        /// </summary>
        LessThan,

        /// <summary>
        /// ">"
        /// </summary>
        GreaterThan,

        /// <summary>
        /// "&lt;="
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// ">="
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// "in"
        /// </summary>
        In,

        /// <summary>
        /// "Contains"
        /// </summary>
        Contains,

        /// <summary>
        /// "like *%"
        /// </summary>
        StartsWith,

        /// <summary>
        /// "like %*"
        /// </summary>
        EndsWith,
    }
}
