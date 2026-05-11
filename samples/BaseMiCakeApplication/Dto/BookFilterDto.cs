using System;
using MiCake.Util.Query.Dynamic;

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
    public class BookFilterDto : IDynamicQueryModel
    {
        /// <summary>
        /// Gets or sets the book name filter (Equal comparison).
        /// </summary>
        [DynamicFilter(OperatorType = ValueOperatorType.Equal)]
        public string BookName { get; set; }

        /// <summary>
        /// Gets or sets the author first name filter (Contains comparison).
        /// </summary>
        [DynamicFilter(OperatorType = ValueOperatorType.Contains, PropertyName = "Author.FirstName")]
        public string AuthorFirstName { get; set; }

        [DynamicFilter(OperatorType = ValueOperatorType.GreaterThanOrEqual)]
        public DateTimeOffset? CreatedAt { get; set; } 

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
