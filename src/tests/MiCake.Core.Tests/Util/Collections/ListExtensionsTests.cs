using System.Collections.Generic;
using MiCake.Util.Collection;
using Xunit;

namespace MiCake.Core.Tests.Util.Collections;

public class ListExtensionsTests
{
    [Fact]
    public void FindIndex_WithExistingItem_ShouldReturnCorrectIndex()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var index = list.FindIndex(x => x == 3);

        // Assert
        Assert.Equal(2, index);
    }

    [Fact]
    public void FindIndex_WithNonExistingItem_ShouldReturnNegativeOne()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var index = list.FindIndex(x => x == 99);

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void FindIndex_WithFirstItem_ShouldReturnZero()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var index = list.FindIndex(x => x == 1);

        // Assert
        Assert.Equal(0, index);
    }

    [Fact]
    public void FindIndex_WithLastItem_ShouldReturnLastIndex()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var index = list.FindIndex(x => x == 5);

        // Assert
        Assert.Equal(4, index);
    }

    [Fact]
    public void FindIndex_WithComplexPredicate_ShouldReturnFirstMatchIndex()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5, 6 };

        // Act
        var index = list.FindIndex(x => x > 3);

        // Assert
        Assert.Equal(3, index); // First item > 3 is at index 3
    }

    [Fact]
    public void AddFirst_ShouldAddItemAtBeginning()
    {
        // Arrange
        var list = new List<int> { 2, 3, 4 };

        // Act
        list.AddFirst(1);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void AddFirst_ToEmptyList_ShouldAddItemAtBeginning()
    {
        // Arrange
        var list = new List<int>();

        // Act
        list.AddFirst(1);

        // Assert
        Assert.Single(list);
        Assert.Equal(1, list[0]);
    }

    [Fact]
    public void AddLast_ShouldAddItemAtEnd()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        list.AddLast(4);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(4, list[3]);
    }

    [Fact]
    public void AddLast_ToEmptyList_ShouldAddItemAtEnd()
    {
        // Arrange
        var list = new List<int>();

        // Act
        list.AddLast(1);

        // Assert
        Assert.Single(list);
        Assert.Equal(1, list[0]);
    }

    [Fact]
    public void InsertAfter_WithExistingItem_ShouldInsertCorrectly()
    {
        // Arrange
        var list = new List<int> { 1, 2, 4 };

        // Act
        list.InsertAfter(2, 3);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(new List<int> { 1, 2, 3, 4 }, list);
    }

    [Fact]
    public void InsertAfter_WithNonExistingItem_ShouldAddFirst()
    {
        // Arrange
        var list = new List<int> { 2, 3, 4 };

        // Act
        list.InsertAfter(99, 1);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(1, list[0]);
    }

    [Fact]
    public void InsertAfter_WithPredicate_WithMatchingItem_ShouldInsertCorrectly()
    {
        // Arrange
        var list = new List<int> { 1, 2, 4 };

        // Act
        list.InsertAfter(x => x == 2, 3);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(new List<int> { 1, 2, 3, 4 }, list);
    }

    [Fact]
    public void InsertAfter_WithPredicate_WithNoMatchingItem_ShouldAddFirst()
    {
        // Arrange
        var list = new List<int> { 2, 3, 4 };

        // Act
        list.InsertAfter(x => x == 99, 1);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(1, list[0]);
    }

    [Fact]
    public void InsertBefore_WithExistingItem_ShouldInsertCorrectly()
    {
        // Arrange
        var list = new List<int> { 1, 3, 4 };

        // Act
        list.InsertBefore(3, 2);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(new List<int> { 1, 2, 3, 4 }, list);
    }

    [Fact]
    public void InsertBefore_WithNonExistingItem_ShouldAddLast()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        list.InsertBefore(99, 4);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(4, list[3]);
    }

    [Fact]
    public void InsertBefore_WithPredicate_WithMatchingItem_ShouldInsertCorrectly()
    {
        // Arrange
        var list = new List<int> { 1, 3, 4 };

        // Act
        list.InsertBefore(x => x == 3, 2);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(new List<int> { 1, 2, 3, 4 }, list);
    }

    [Fact]
    public void InsertBefore_WithPredicate_WithNoMatchingItem_ShouldAddLast()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        list.InsertBefore(x => x == 99, 4);

        // Assert
        Assert.Equal(4, list.Count);
        Assert.Equal(4, list[3]);
    }
}
