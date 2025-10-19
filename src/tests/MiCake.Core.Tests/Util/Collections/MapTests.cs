using System;
using System.Collections.Generic;
using MiCake.Core.Util.Collections;
using Xunit;

namespace MiCake.Core.Tests.Util.Collections;

public class MapTests
{
    [Fact]
    public void Add_ShouldAddBidirectionalMapping()
    {
        // Arrange
        var map = new Map<string, int>();

        // Act
        map.Add("one", 1);
        map.Add("two", 2);
        map.Add("three", 3);

        // Assert
        Assert.Equal(1, map.Forward["one"]);
        Assert.Equal(2, map.Forward["two"]);
        Assert.Equal(3, map.Forward["three"]);
        
        Assert.Equal("one", map.Reverse[1]);
        Assert.Equal("two", map.Reverse[2]);
        Assert.Equal("three", map.Reverse[3]);
    }

    [Fact]
    public void Add_WithDuplicateForwardKey_ShouldThrowException()
    {
        // Arrange
        var map = new Map<string, int>();
        map.Add("one", 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => map.Add("one", 2));
    }

    [Fact]
    public void Add_WithDuplicateReverseKey_ShouldThrowException()
    {
        // Arrange
        var map = new Map<string, int>();
        map.Add("one", 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => map.Add("two", 1));
    }

    [Fact]
    public void Clear_ShouldClearAllMappings()
    {
        // Arrange
        var map = new Map<string, int>();
        map.Add("one", 1);
        map.Add("two", 2);

        // Act
        map.Clear();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => map.Forward["one"]);
        Assert.Throws<KeyNotFoundException>(() => map.Reverse[1]);
    }

    [Fact]
    public void Forward_Indexer_ShouldReturnValue()
    {
        // Arrange
        var map = new Map<string, int>();
        map.Add("test", 42);

        // Act
        var value = map.Forward["test"];

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void Forward_Indexer_ShouldThrowKeyNotFoundExceptionForNonExistingKey()
    {
        // Arrange
        var map = new Map<string, int>();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => map.Forward["nonexistent"]);
    }

    [Fact]
    public void Reverse_Indexer_ShouldReturnKey()
    {
        // Arrange
        var map = new Map<string, int>();
        map.Add("test", 42);

        // Act
        var key = map.Reverse[42];

        // Assert
        Assert.Equal("test", key);
    }

    [Fact]
    public void Reverse_Indexer_ShouldThrowKeyNotFoundExceptionForNonExistingValue()
    {
        // Arrange
        var map = new Map<string, int>();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => map.Reverse[99]);
    }

    [Fact]
    public void Forward_TryGetValue_WithExistingKey_ShouldReturnTrueAndValue()
    {
        // Arrange
        var map = new Map<string, int>();
        map.Add("test", 42);

        // Act
        var result = map.Forward.TryGetValue("test", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void Forward_TryGetValue_WithNonExistingKey_ShouldReturnFalseAndDefault()
    {
        // Arrange
        var map = new Map<string, int>();

        // Act
        var result = map.Forward.TryGetValue("nonexistent", out var value);

        // Assert
        Assert.False(result);
        Assert.Equal(default(int), value);
    }

    [Fact]
    public void Reverse_TryGetValue_WithExistingValue_ShouldReturnTrueAndKey()
    {
        // Arrange
        var map = new Map<string, int>();
        map.Add("test", 42);

        // Act
        var result = map.Reverse.TryGetValue(42, out var key);

        // Assert
        Assert.True(result);
        Assert.Equal("test", key);
    }

    [Fact]
    public void Reverse_TryGetValue_WithNonExistingValue_ShouldReturnFalseAndDefault()
    {
        // Arrange
        var map = new Map<string, int>();

        // Act
        var result = map.Reverse.TryGetValue(99, out var key);

        // Assert
        Assert.False(result);
        Assert.Equal(default(string), key);
    }

    [Fact]
    public void Forward_Indexer_Setter_ShouldUpdateMapping()
    {
        // Arrange
        var map = new Map<string, int>();
        map.Add("test", 42);

        // Act
        map.Forward["test"] = 100;

        // Assert
        Assert.Equal(100, map.Forward["test"]);
        // Note: The Reverse mapping will not be updated automatically
        // This is a limitation of the current implementation
    }

    [Fact]
    public void Map_WithComplexTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var map = new Map<int, string>();
        map.Add(1, "one");
        map.Add(2, "two");
        map.Add(3, "three");

        // Act & Assert
        Assert.Equal("one", map.Forward[1]);
        Assert.Equal(1, map.Reverse["one"]);
        Assert.Equal("two", map.Forward[2]);
        Assert.Equal(2, map.Reverse["two"]);
    }

    [Fact]
    public void Map_WithMultipleItems_ShouldMaintainBidirectionalMapping()
    {
        // Arrange
        var map = new Map<int, string>();
        for (int i = 1; i <= 10; i++)
        {
            map.Add(i, $"value{i}");
        }

        // Act & Assert
        for (int i = 1; i <= 10; i++)
        {
            Assert.Equal($"value{i}", map.Forward[i]);
            Assert.Equal(i, map.Reverse[$"value{i}"]);
        }
    }
}
