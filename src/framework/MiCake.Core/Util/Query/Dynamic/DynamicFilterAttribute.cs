using System;

namespace MiCake.Util.Query.Dynamic;

/// <summary>
/// An attribute to mark properties for dynamic filtering in Linq queries.
/// <para>
/// When your query model has implemented the <see cref="IDynamicQueryObj"/> interface,
/// this attribute can be used to specify which properties should be included in dynamic filtering.
/// </para>
/// <para>
/// If there are more than one property with this attribute, please also use <see cref="DynamicFilterJoinAttribute"/> to specify how these filters should be combined.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DynamicFilterAttribute : Attribute
{
    /// <summary>
    /// The name of the property to filter on.
    /// If null, the property name will be used.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// The operator type for the filter.
    /// This defines how the filter values are compared against the property.
    /// </summary>
    public FilterOperatorType OperatorType { get; set; } = FilterOperatorType.Equal;
}

/// <summary>
/// An attribute to specify the join type for dynamic filters.
/// <para>
/// This can be applied to classes that implement <see cref="IDynamicQueryObj"/>.
/// </para>
/// <para>
/// When the properties in the class has been decorated with <see cref="DynamicFilterAttribute"/>, this attribute defines how the filters are combined.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DynamicFilterJoinAttribute : Attribute
{
    /// <summary>
    /// The join type for the filter.
    /// This defines how multiple filters within the same group are combined.
    /// Default is <see cref="FilterJoinType.And"/>.
    /// </summary>
    public FilterJoinType JoinType { get; set; } = FilterJoinType.And;
}

