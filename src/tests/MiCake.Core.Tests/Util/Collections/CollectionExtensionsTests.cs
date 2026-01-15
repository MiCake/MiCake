using MiCake.Util.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MiCake.Core.Tests.Util.Collections;

/// <summary>
/// Tests for CollectionExtensions
/// </summary>
public class CollectionExtensionsTests
{
    #region IsNullOrEmpty Tests

    [Fact]
    public void IsNullOrEmpty_WithNullCollection_ShouldReturnTrue()
    {
        // Arrange
        ICollection<int> collection = null;

        // Act
        var result = collection.IsNullOrEmpty();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_WithEmptyCollection_ShouldReturnTrue()
    {
        // Arrange
        ICollection<int> collection = new List<int>();

        // Act
        var result = collection.IsNullOrEmpty();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_WithNonEmptyCollection_ShouldReturnFalse()
    {
        // Arrange
        ICollection<int> collection = new List<int> { 1, 2, 3 };

        // Act
        var result = collection.IsNullOrEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region AddIfNotContains Tests (Single Item)

    [Fact]
    public void AddIfNotContains_WithNewItem_ShouldAddAndReturnTrue()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var item = 4;

        // Act
        var result = collection.AddIfNotContains(item);

        // Assert
        Assert.True(result);
        Assert.Contains(item, collection);
        Assert.Equal(4, collection.Count);
    }

    [Fact]
    public void AddIfNotContains_WithExistingItem_ShouldNotAddAndReturnFalse()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var item = 2;

        // Act
        var result = collection.AddIfNotContains(item);

        // Assert
        Assert.False(result);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void AddIfNotContains_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        ICollection<int> collection = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => collection.AddIfNotContains(5));
    }

    #endregion

    #region AddIfNotContains Tests (With Predicate)

    [Fact]
    public void AddIfNotContains_WithPredicate_WhenNotExists_ShouldAddAndReturnTrue()
    {
        // Arrange
        var collection = new List<TestItem>
        {
            new TestItem { Id = 1, Name = "Item1" },
            new TestItem { Id = 2, Name = "Item2" }
        };
        var newItem = new TestItem { Id = 3, Name = "Item3" };

        // Act
        var result = collection.AddIfNotContains(newItem, x => x.Id == newItem.Id);

        // Assert
        Assert.True(result);
        Assert.Contains(newItem, collection);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void AddIfNotContains_WithPredicate_WhenExists_ShouldNotAddAndReturnFalse()
    {
        // Arrange
        var collection = new List<TestItem>
        {
            new TestItem { Id = 1, Name = "Item1" },
            new TestItem { Id = 2, Name = "Item2" }
        };
        var newItem = new TestItem { Id = 2, Name = "Item2_Updated" };

        // Act
        var result = collection.AddIfNotContains(newItem, x => x.Id == newItem.Id);

        // Assert
        Assert.False(result);
        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void AddIfNotContains_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            collection.AddIfNotContains(4, null));
    }

    #endregion

    #region AddIfNotContains Tests (Multiple Items)

    [Fact]
    public void AddIfNotContains_WithMultipleItems_ShouldAddOnlyNewItems()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var itemsToAdd = new[] { 2, 3, 4, 5 };

        // Act
        var addedItems = collection.AddIfNotContains(itemsToAdd).ToList();

        // Assert
        Assert.Equal(2, addedItems.Count);
        Assert.Contains(4, addedItems);
        Assert.Contains(5, addedItems);
        Assert.Equal(5, collection.Count);
    }

    [Fact]
    public void AddIfNotContains_WithAllExistingItems_ShouldAddNone()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var itemsToAdd = new[] { 1, 2, 3 };

        // Act
        var addedItems = collection.AddIfNotContains(itemsToAdd).ToList();

        // Assert
        Assert.Empty(addedItems);
        Assert.Equal(3, collection.Count);
    }

    #endregion

    #region AddIfNotContains Tests (With Factory)

    [Fact]
    public void AddIfNotContains_WithFactory_WhenNotExists_ShouldCreateAndAdd()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var factoryCalled = false;

        // Act
        var result = collection.AddIfNotContains(
            x => x == 4,
            () =>
            {
                factoryCalled = true;
                return 4;
            });

        // Assert
        Assert.True(result);
        Assert.True(factoryCalled);
        Assert.Contains(4, collection);
        Assert.Equal(4, collection.Count);
    }

    [Fact]
    public void AddIfNotContains_WithFactory_WhenExists_ShouldNotCallFactory()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var factoryCalled = false;

        // Act
        var result = collection.AddIfNotContains(
            x => x == 2,
            () =>
            {
                factoryCalled = true;
                return 2;
            });

        // Assert
        Assert.False(result);
        Assert.False(factoryCalled);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void AddIfNotContains_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            collection.AddIfNotContains(x => x == 4, null));
    }

    #endregion

    #region RemoveAll Tests

    [Fact]
    public void RemoveAll_WithMatchingItems_ShouldRemoveAndReturnThem()
    {
        // Arrange
        ICollection<int> collection = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var removedItems = collection.RemoveAll(x => x > 3);

        // Assert
        Assert.Equal(2, removedItems.Count);
        Assert.True(removedItems.Contains(4));
        Assert.True(removedItems.Contains(5));
        Assert.Equal(3, collection.Count);
        Assert.False(collection.Contains(4));
        Assert.False(collection.Contains(5));
    }

    [Fact]
    public void RemoveAll_WithNoMatchingItems_ShouldReturnEmptyList()
    {
        // Arrange
        ICollection<int> collection = new List<int> { 1, 2, 3 };

        // Act
        var removedItems = collection.RemoveAll(x => x > 10);

        // Assert
        Assert.Empty(removedItems);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void RemoveAll_RemovingAllItems_ShouldEmptyCollection()
    {
        // Arrange
        ICollection<int> collection = new List<int> { 1, 2, 3 };

        // Act
        var removedItems = collection.RemoveAll(x => true);

        // Assert
        Assert.Equal(3, removedItems.Count);
        Assert.Empty(collection);
    }

    #endregion

    #region Helper Classes

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    #endregion
}
