namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// Names of the EF Core named query filters that MiCake applies.
    /// Use these names with <c>IgnoreQueryFilters</c> to bypass one MiCake filter while keeping other filters.
    /// </summary>
    public static class MiCakeQueryFilterNames
    {
        /// <summary>
        /// The soft-delete filter applied to entities that implement <c>ISoftDeletable</c>.
        /// </summary>
        public const string SoftDelete = "MiCake.SoftDelete";
    }
}
