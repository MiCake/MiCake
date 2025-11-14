using MiCake.Util.LinqFilter;

namespace BaseMiCakeApplication.Dto
{
    /// <summary>
    /// DTO for filtering books with dynamic query support.
    /// </summary>
    /// <remarks>
    /// This DTO demonstrates the MiCake LinqFilter feature which allows
    /// dynamic filtering based on the provided properties.
    /// </remarks>
    [DynamicFilterJoin(JoinType = FilterJoinType.Or)]
    public class BookFilterDto : IDynamicQueryObj
    {
        /// <summary>
        /// Gets or sets the book name filter (Equal comparison).
        /// </summary>
        [DynamicFilter(OperatorType = FilterOperatorType.Equal)]
        public string BookName { get; set; }

        /// <summary>
        /// Gets or sets the author first name filter (Contains comparison).
        /// </summary>
        [DynamicFilter(OperatorType = FilterOperatorType.Contains)]
        public string AuthorFirstName { get; set; }

        /// <summary>
        /// Gets or sets the author last name filter (Contains comparison).
        /// </summary>
        [DynamicFilter(OperatorType = FilterOperatorType.Contains)]
        public string AuthorLastName { get; set; }

        /// <summary>
        /// Gets or sets the page number for pagination (default: 1).
        /// </summary>
        public int? PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size for pagination (default: 10).
        /// </summary>
        public int? PageSize { get; set; } = 10;
    }
}
