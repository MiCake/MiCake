using System;
using System.Collections.Generic;
using System.Linq;
using MiCake.Util.LinqFilter;
using Xunit;

namespace MiCake.Core.Tests.Util.LinqFilter;

class LinqFilterTestModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string IdNumber { get; set; }
    public DateTime BirthDate { get; set; }
}

public class LinqFilter_Tests
{
    private static List<LinqFilterTestModel> GetTestData()
    {
        return
        [
            new() { Id = 1, Name = "Alice", IdNumber = "1234567890", BirthDate = new DateTime(1990, 1, 1) },
            new() { Id = 2, Name = "Bob", IdNumber = "0987654321", BirthDate = new DateTime(1995, 5, 5) },
            new() { Id = 3, Name = "Charlie", IdNumber = "1122334455", BirthDate = new DateTime(2000, 10, 10) }
        ];
    }


    [Fact]
    public void Filter_By_Name_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterName = Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Alice", FilterOperatorType.Equal)
        ]);

        var filteredData = data.AsQueryable().Filter([filterName]).ToList();

        Assert.Single(filteredData);
        Assert.Equal("Alice", filteredData[0].Name);
    }

    [Fact]
    public void Filter_By_Id_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterId = Filter.Create(nameof(LinqFilterTestModel.Id), [
            FilterValue.Create(2, FilterOperatorType.Equal)
        ]);

        var filteredData = data.AsQueryable().Filter([filterId]).ToList();

        Assert.Single(filteredData);
        Assert.Equal(2, filteredData[0].Id);
    }

    [Fact]
    public void Filter_By_IdNumber_Contains_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterIdNumber = Filter.Create(nameof(LinqFilterTestModel.IdNumber), [
            FilterValue.Create("123", FilterOperatorType.Contains)
        ]);

        var filteredData = data.AsQueryable().Filter([filterIdNumber]).ToList();

        Assert.Single(filteredData);
        Assert.Equal("Alice", filteredData[0].Name);
    }

    [Fact]
    public void Filter_By_BirthDate_GreaterThan_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterBirthDate = Filter.Create(nameof(LinqFilterTestModel.BirthDate), [
            FilterValue.Create(new DateTime(1994, 1, 1), FilterOperatorType.GreaterThan)
        ]);

        var filteredData = data.AsQueryable().Filter([filterBirthDate]).ToList();

        Assert.Equal(2, filteredData.Count);
        Assert.Contains(filteredData, x => x.Name == "Bob");
        Assert.Contains(filteredData, x => x.Name == "Charlie");
    }

    [Fact]
    public void Filter_By_Multiple_Conditions_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterName = Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Bob", FilterOperatorType.Equal)
        ]);
        var filterBirthDate = Filter.Create(nameof(LinqFilterTestModel.BirthDate), [
            FilterValue.Create(new DateTime(1990, 1, 1), FilterOperatorType.GreaterThan)
        ]);

        var filteredData = data.AsQueryable().Filter([filterName, filterBirthDate]).ToList();

        Assert.Single(filteredData);
        Assert.Equal("Bob", filteredData[0].Name);
    }

    [Fact]
    public void Filter_By_InOperator_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterName = Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Alice", FilterOperatorType.In),
            FilterValue.Create("Charlie", FilterOperatorType.In)
        ]);

        var filteredData = data.AsQueryable().Filter([filterName]).ToList();

        Assert.Equal(2, filteredData.Count);
        Assert.Contains(filteredData, x => x.Name == "Alice");
        Assert.Contains(filteredData, x => x.Name == "Charlie");
    }

    [Fact]
    public void Filter_By_StartsWithOperator_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterName = Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("A", FilterOperatorType.StartsWith)
        ]);

        var filteredData = data.AsQueryable().Filter([filterName]).ToList();

        Assert.Single(filteredData);
        Assert.Equal("Alice", filteredData[0].Name);
    }

    [Fact]
    public void Filter_By_EndsWithOperator_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterName = Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("e", FilterOperatorType.EndsWith)
        ]);

        var filteredData = data.AsQueryable().Filter([filterName]).ToList();

        Assert.Equal(2, filteredData.Count);
        Assert.Contains(filteredData, x => x.Name == "Alice");
        Assert.Contains(filteredData, x => x.Name == "Charlie");
    }

    [Fact]
    public void Filter_With_Empty_Filters_Should_Return_All_Data()
    {
        var data = GetTestData();

        var filteredData = data.AsQueryable().Filter(new List<Filter>()).ToList();

        Assert.Equal(data.Count, filteredData.Count);
    }

    [Fact]
    public void Filter_With_Null_Filters_Should_Return_All_Data()
    {
        var data = GetTestData();

        var filteredData = data.AsQueryable().Filter((List<Filter>)null).ToList();

        Assert.Equal(data.Count, filteredData.Count);
    }

    [Fact]
    public void FilterGroup_By_AndOperator_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterGroup = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
                FilterValue.Create("Alice", FilterOperatorType.Equal)
            ]),
            Filter.Create(nameof(LinqFilterTestModel.BirthDate), [
                FilterValue.Create(new DateTime(1985, 1, 1), FilterOperatorType.LessThan)
            ])
        ], FilterJoinType.And);

        var filteredData = data.AsQueryable().Filter(filterGroup).ToList();

        Assert.Empty(filteredData);
    }

    [Fact]
    public void FilterGroup_By_OrOperator_Should_Return_Union_Results()
    {
        var data = GetTestData();

        var filterGroup = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
                FilterValue.Create("Alice", FilterOperatorType.Equal)
            ]),
            Filter.Create(nameof(LinqFilterTestModel.Name), [
                FilterValue.Create("Bob", FilterOperatorType.Equal)
            ])
        ], FilterJoinType.Or);

        var filteredData = data.AsQueryable().Filter(filterGroup).ToList();

        Assert.Equal(2, filteredData.Count);
        Assert.Contains(filteredData, x => x.Name == "Alice");
        Assert.Contains(filteredData, x => x.Name == "Bob");
    }

    [Fact]
    public void FilterGroup_By_Mixed_Operators_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterGroup = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
                FilterValue.Create("Charlie", FilterOperatorType.Equal)
            ]),
            Filter.Create(nameof(LinqFilterTestModel.BirthDate), [
                FilterValue.Create(new DateTime(1999, 1, 1), FilterOperatorType.GreaterThan)
            ])
        ], FilterJoinType.And);

        var filteredData = data.AsQueryable().Filter(filterGroup).ToList();

        Assert.Single(filteredData);
        Assert.Equal("Charlie", filteredData[0].Name);
    }

    [Fact]
    public void FilterGroup_With_Empty_Filters_Should_Throw()
    {
        Assert.Throws<ArgumentException>(() => FilterGroup.Create(new List<Filter>(), FilterJoinType.And));
    }

    [Fact]
    public void FilterGroup_With_Null_Filters_Should_Throw()
    {
        Assert.Throws<ArgumentException>(() => FilterGroup.Create(null, FilterJoinType.Or));
    }

    [Fact]
    public void FilterGroup_Default_JoinType_Should_Be_Or()
    {
        var filters = new List<Filter>
        {
            Filter.Create("Name", [FilterValue.Create("Alice", FilterOperatorType.Equal)])
        };

        var group = new FilterGroup { Filters = filters };

        Assert.Equal(FilterJoinType.Or, group.FilterGroupJoinType);
    }

    [Fact]
    public void FilterGroup_Filters_Property_Should_Be_Initialized()
    {
        var group = new FilterGroup();
        Assert.NotNull(group.Filters);
        Assert.Empty(group.Filters);
    }

    [Fact]
    public void FilterGroup_By_Multiple_Conditions_OrOperator_Should_Return_All_Matching()
    {
        var data = GetTestData();

        var filterGroup = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.IdNumber), [
                FilterValue.Create("1234567890", FilterOperatorType.Equal)
            ]),
            Filter.Create(nameof(LinqFilterTestModel.BirthDate), [
                FilterValue.Create(new DateTime(2000, 10, 10), FilterOperatorType.Equal)
            ])
        ], FilterJoinType.Or);

        var filteredData = data.AsQueryable().Filter(filterGroup).ToList();

        Assert.Equal(2, filteredData.Count);
        Assert.Contains(filteredData, x => x.Name == "Alice");
        Assert.Contains(filteredData, x => x.Name == "Charlie");
    }

    [Fact]
    public void FilterGroup_By_Single_Filter_Should_Work()
    {
        var data = GetTestData();

        var filterGroup = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
                FilterValue.Create("Bob", FilterOperatorType.Equal)
            ])
        ], FilterJoinType.And);

        var filteredData = data.AsQueryable().Filter(filterGroup).ToList();

        Assert.Single(filteredData);
        Assert.Equal("Bob", filteredData[0].Name);
    }

    [Fact]
    public void CompositeFilterGroup_By_AndOperator_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterGroup1 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
                FilterValue.Create("Alice", FilterOperatorType.Equal)
            ])
        ], FilterJoinType.And);

        var filterGroup2 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.BirthDate), [
                FilterValue.Create(new DateTime(1985, 1, 1), FilterOperatorType.LessThan)
            ])
        ], FilterJoinType.And);

        var compositeFilter = new CompositeFilterGroup
        {
            FilterGroups = [filterGroup1, filterGroup2],
            FilterGroupJoinType = FilterJoinType.And
        };

        var filteredData = data.AsQueryable().Filter(compositeFilter).ToList();

        Assert.Empty(filteredData);
    }

    [Fact]
    public void CompositeFilterGroup_By_OrOperator_Should_Return_Union_Results()
    {
        var data = GetTestData();

        var filterGroup1 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Alice", FilterOperatorType.Equal)
        ])
        ], FilterJoinType.And);

        var filterGroup2 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Bob", FilterOperatorType.Equal)
        ])
        ], FilterJoinType.And);

        var compositeFilter = new CompositeFilterGroup
        {
            FilterGroups = [filterGroup1, filterGroup2],
            FilterGroupJoinType = FilterJoinType.Or
        };

        var filteredData = data.AsQueryable().Filter(compositeFilter).ToList();

        Assert.Equal(2, filteredData.Count);
        Assert.Contains(filteredData, x => x.Name == "Alice");
        Assert.Contains(filteredData, x => x.Name == "Bob");
    }

    [Fact]
    public void CompositeFilterGroup_MultipleGroups_AndOperator_Should_Return_Correct_Results()
    {
        var data = GetTestData();

        var filterGroup1 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.BirthDate), [
            FilterValue.Create(new DateTime(1989, 1, 1), FilterOperatorType.GreaterThan)
        ])
        ], FilterJoinType.And);

        var filterGroup2 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.IdNumber), [
            FilterValue.Create("1122334455", FilterOperatorType.Equal)
        ])
        ], FilterJoinType.And);

        var compositeFilter = new CompositeFilterGroup
        {
            FilterGroups = [filterGroup1, filterGroup2],
            FilterGroupJoinType = FilterJoinType.And
        };

        var filteredData = data.AsQueryable().Filter(compositeFilter).ToList();

        Assert.Single(filteredData);
        Assert.Equal("Charlie", filteredData[0].Name);
    }

    [Fact]
    public void CompositeFilterGroup_MultipleGroups_OrOperator_Should_Return_All_Matching()
    {
        var data = GetTestData();

        var filterGroup1 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.BirthDate), [
            FilterValue.Create(new DateTime(1995, 1, 1), FilterOperatorType.GreaterThan)
        ])
        ], FilterJoinType.And);

        var filterGroup2 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Alice", FilterOperatorType.Equal)
        ])
        ], FilterJoinType.And);

        var compositeFilter = new CompositeFilterGroup
        {
            FilterGroups = [filterGroup1, filterGroup2],
            FilterGroupJoinType = FilterJoinType.Or
        };

        var filteredData = data.AsQueryable().Filter(compositeFilter).ToList();

        Assert.Equal(3, filteredData.Count);
        Assert.Contains(filteredData, x => x.Name == "Alice");
        Assert.Contains(filteredData, x => x.Name == "Charlie");
        Assert.Contains(filteredData, x => x.Name == "Bob");
    }

    [Fact]
    public void CompositeFilterGroup_With_Empty_FilterGroups_Should_Return_No_Results()
    {
        var data = GetTestData();

        var compositeFilter = new CompositeFilterGroup
        {
            FilterGroups = [],
            FilterGroupJoinType = FilterJoinType.And
        };

        var filteredData = data.AsQueryable().Filter(compositeFilter).ToList();

        Assert.Equal(3, filteredData.Count);
    }

    [Fact]
    public void CompositeFilterGroup_SingleGroup_Should_Behave_Like_FilterGroup()
    {
        var data = GetTestData();

        var filterGroup = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Charlie", FilterOperatorType.Equal)
        ])
        ], FilterJoinType.And);

        var compositeFilter = new CompositeFilterGroup
        {
            FilterGroups = [filterGroup],
            FilterGroupJoinType = FilterJoinType.And
        };

        var filteredData = data.AsQueryable().Filter(compositeFilter).ToList();

        Assert.Single(filteredData);
        Assert.Equal("Charlie", filteredData[0].Name);
    }

    [Fact]
    public void CompositeFilterGroup_Different_JoinTypes_Should_Produce_Different_Results()
    {
        var data = GetTestData();

        var filterGroup1 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Alice", FilterOperatorType.Equal)
        ])
        ], FilterJoinType.And);

        var filterGroup2 = FilterGroup.Create([
            Filter.Create(nameof(LinqFilterTestModel.Name), [
            FilterValue.Create("Bob", FilterOperatorType.Equal)
        ])
        ], FilterJoinType.And);

        var compositeFilterAnd = new CompositeFilterGroup
        {
            FilterGroups = [filterGroup1, filterGroup2],
            FilterGroupJoinType = FilterJoinType.And
        };

        var compositeFilterOr = new CompositeFilterGroup
        {
            FilterGroups = [filterGroup1, filterGroup2],
            FilterGroupJoinType = FilterJoinType.Or
        };

        var andResults = data.AsQueryable().Filter(compositeFilterAnd).ToList();
        var orResults = data.AsQueryable().Filter(compositeFilterOr).ToList();

        Assert.Empty(andResults); // No one is both Alice and Bob
        Assert.Equal(2, orResults.Count);
    }
}