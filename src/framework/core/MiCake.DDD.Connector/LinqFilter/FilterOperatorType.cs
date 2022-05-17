namespace MiCake.Cord.LinqFilter
{
    public enum FilterOperatorType
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
