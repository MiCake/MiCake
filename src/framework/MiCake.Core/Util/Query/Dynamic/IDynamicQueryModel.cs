namespace MiCake.Util.Query.Dynamic;

/// <summary>
/// Marker interface for objects that support dynamic query generation.
/// </summary>
/// <remarks>
/// <para>
/// When a class implements this interface, its properties can be decorated with 
/// <see cref="DynamicFilterAttribute"/> to enable attribute-based query filter generation.
/// </para>
/// <para>
/// Use the <see cref="DynamicQueryGeneratorExtensions.GenerateFilterGroup{T}"/> method
/// to convert an instance into a <see cref="FilterGroup"/> that can be applied to LINQ queries.
/// </para>
/// <example>
/// <code>
/// [DynamicFilterJoinAttribute(JoinType = FilterJoinType.And)]
/// public class UserQuery : IDynamicQueryModel
/// {
///     [DynamicFilter(OperatorType = FilterOperatorType.Contains)]
///     public string? Name { get; set; }
///     
///     [DynamicFilter(OperatorType = FilterOperatorType.GreaterThan)]
///     public int? MinAge { get; set; }
/// }
/// 
/// // Usage:
/// var query = new UserQuery { Name = "John", MinAge = 18 };
/// var filterGroup = query.GenerateFilterGroup();
/// var results = dbContext.Users.Filter(filterGroup).ToList();
/// </code>
/// </example>
/// </remarks>
public interface IDynamicQueryModel
{

}
