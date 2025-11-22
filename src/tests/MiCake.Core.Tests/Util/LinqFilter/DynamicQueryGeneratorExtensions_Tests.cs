using System.Collections.Generic;
using System.Linq;
using MiCake.Util.Query.Dynamic;
using Xunit;

namespace MiCake.Core.Tests.Util.LinqFilter;

public class TestQueryObj : IDynamicQueryObj
{
    [DynamicFilter(PropertyName = "Age", OperatorType = ValueOperatorType.GreaterThan)]
    public int? Age { get; set; }

    [DynamicFilter(PropertyName = "Tags", OperatorType = ValueOperatorType.In)]
    public List<string>? Tags { get; set; }

    [DynamicFilter(PropertyName = "Name", OperatorType = ValueOperatorType.Contains)]
    public string? Name { get; set; }

    [DynamicFilter(PropertyName = "Scores", OperatorType = ValueOperatorType.In)]
    public int[]? Scores { get; set; }
}

public class SimpleQueryTestObj : IDynamicQueryObj
{
    public int Id { get; set; }

    [DynamicFilter(PropertyName = "Name", OperatorType = ValueOperatorType.Equal)]
    public string Name { get; set; }
}

public class QueryTestModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Tags { get; set; }
}

public class DynamicQueryGeneratorExtensions_Tests
{
    public static List<QueryTestModel> GetTestObjects()
    {
        return
        [
            new QueryTestModel { Id = 1, Name = "MiCake", Age = 30, Tags = ["A", "B"] },
            new QueryTestModel { Id = 2, Name = "Cake", Age = 25, Tags = ["B", "C"] },
            new QueryTestModel { Id = 3, Name = "MiCake Framework", Age = 35, Tags = ["A", "C"] },
            new QueryTestModel { Id = 4, Name = "Framework", Age = 40, Tags = ["B", "D"] },
        ];
    }

    [Fact]
    public void GenerateFilterGroup_ShouldReturnCorrectFilters()
    {
        var obj = new TestQueryObj { Age = 25, Tags = ["A", "B"], Name = "MiCake", Scores = [1, 2] };
        var group = obj.GenerateFilterGroup();

        Assert.Equal(FilterJoinType.And, group.FiltersJoinType);
        Assert.Equal(4, group.Filters.Count);
        Assert.Contains(group.Filters, f => f.PropertyName == "Age");
        Assert.Contains(group.Filters, f => f.PropertyName == "Tags");
        Assert.Contains(group.Filters, f => f.PropertyName == "Name");
        Assert.Contains(group.Filters, f => f.PropertyName == "Scores");
    }

    [Fact]
    public void GenerateFilterGroup_ShouldSkipNullOrEmptyCollections()
    {
        var obj = new TestQueryObj { Age = 0, Tags = [], Name = null, Scores = null };
        var group = obj.GenerateFilterGroup();

        Assert.Single(group.Filters);
    }

    [Fact]
    public void GenerateFilterGroup_ShouldHandleArrayProperties()
    {
        var obj = new TestQueryObj { Scores = [10, 20] };
        var group = obj.GenerateFilterGroup();

        Assert.Contains(group.Filters, f => f.PropertyName == "Scores");
    }

    [Fact]
    public void GenerateFilterGroup_ShouldHandleNullValues()
    {
        var obj = new TestQueryObj { Name = null, Tags = null, Scores = null };
        var group = obj.GenerateFilterGroup();

        Assert.Empty(group.Filters);
    }

    [Fact]
    public void GenerateFilterGroup_ShouldHandleSingleValueProperties()
    {
        var obj = new TestQueryObj { Age = 30 };
        var group = obj.GenerateFilterGroup();

        Assert.Single(group.Filters);
        Assert.Equal("Age", group.Filters[0].PropertyName);
    }

    [Fact]
    public void ApplyFilterGroup_ShouldApplyFiltersCorrectly()
    {
        var obj = GetTestObjects().AsQueryable();

        var simpleQueryInstance = new SimpleQueryTestObj { Name = "MiCake" };
        var group = simpleQueryInstance.GenerateFilterGroup();

        var filteredResults = obj.Filter(group);

        Assert.Single(filteredResults);
        Assert.Equal("MiCake", filteredResults.First().Name);
    }

    [Fact]
    public void GenerateFilterGroup_ShouldAccept_IEnumerable_Not_ICollection()
    {
        var obj = new NonCollectionQueryObj { Numbers = Enumerable.Range(1, 1).Select(i => i) };

        var group = obj.GenerateFilterGroup();

        Assert.Contains(group.Filters, f => f.PropertyName == "Numbers");
    }

    [Fact]
    public void GenerateFilterGroup_Should_Skip_Empty_IEnumerable()
    {
        var obj = new NonCollectionQueryObj { Numbers = Enumerable.Empty<int>().Where(i => false) };

        var group = obj.GenerateFilterGroup();

        Assert.DoesNotContain(group.Filters, f => f.PropertyName == "Numbers");
    }
}

public class NonCollectionQueryObj : IDynamicQueryObj
{
    [DynamicFilter(PropertyName = "Numbers", OperatorType = ValueOperatorType.In)]
    public IEnumerable<int>? Numbers { get; set; }
}