using System;
using System.Collections.Generic;
using MiCake.Core.Util.LinqFilter;

namespace BaseMiCakeApplication.Dto;

[DynamicFilterJoin(JoinType = FilterJoinType.Or)]
public class BookFilterDto : IDynamicQueryObj
{
    [DynamicFilter(OperatorType = FilterOperatorType.Equal)]
    public string? BookName { get; set; }

    [DynamicFilter(OperatorType = FilterOperatorType.GreaterThanOrEqual)]
    public DateTime PublishDate { get; set; }
}
