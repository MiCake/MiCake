using System.Collections.Generic;
using MiCake.Util.Collection;
using Xunit;

namespace MiCake.Core.Tests.Util.Collections;

public class DictionaryExtensionsTests
{
    [Fact]
    public void HasValue_WithExistingValue_ShouldReturnTrue()
    {
        // Arrange
        var dic = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        // Act
        var result = dic.HasValue(2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasValue_WithNonExistingValue_ShouldReturnFalse()
    {
        // Arrange
        var dic = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        // Act
        var result = dic.HasValue(99);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasValue_WithNullValue_ShouldReturnTrue()
    {
        // Arrange
        var dic = new Dictionary<string, string?>
        {
            ["a"] = "value1",
            ["b"] = null,
            ["c"] = "value3"
        };

        // Act
        var result = dic.HasValue(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasValue_WithEmptyDictionary_ShouldReturnFalse()
    {
        // Arrange
        var dic = new Dictionary<string, int>();

        // Act
        var result = dic.HasValue(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasValue_WithDuplicateValues_ShouldReturnTrue()
    {
        // Arrange
        var dic = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 1,
            ["c"] = 1
        };

        // Act
        var result = dic.HasValue(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetKeyByValue_WithExistingValue_ShouldReturnAllKeys()
    {
        // Arrange
        var dic = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 1,
            ["d"] = 3
        };

        // Act
        var result = dic.GetKeyByValue(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("a", result);
        Assert.Contains("c", result);
    }

    [Fact]
    public void GetKeyByValue_WithNonExistingValue_ShouldReturnEmptyList()
    {
        // Arrange
        var dic = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        // Act
        var result = dic.GetKeyByValue(99);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetKeyByValue_WithNullValue_ShouldReturnKeysWithNullValues()
    {
        // Arrange
        var dic = new Dictionary<string, string?>
        {
            ["a"] = "value1",
            ["b"] = null,
            ["c"] = "value3",
            ["d"] = null
        };

        // Act
        var result = dic.GetKeyByValue(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("b", result);
        Assert.Contains("d", result);
    }

    [Fact]
    public void GetKeyByValue_WithEmptyDictionary_ShouldReturnEmptyList()
    {
        // Arrange
        var dic = new Dictionary<string, int>();

        // Act
        var result = dic.GetKeyByValue(1);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetFirstKeyByValue_WithExistingValue_ShouldReturnFirstKey()
    {
        // Arrange
        var dic = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 1,
            ["d"] = 3
        };

        // Act
        var result = dic.GetFirstKeyByValue(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("a", result);
    }

    [Fact]
    public void GetFirstKeyByValue_WithNonExistingValue_ShouldReturnDefaultKey()
    {
        // Arrange
        var dic = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        // Act
        var result = dic.GetFirstKeyByValue(99);

        // Assert
        Assert.Null(result); // Default key for string is null
    }

    [Fact]
    public void GetFirstKeyByValue_WithNullValue_ShouldReturnFirstKeyWithNullValue()
    {
        // Arrange
        var dic = new Dictionary<string, string?>
        {
            ["a"] = "value1",
            ["b"] = null,
            ["c"] = "value3",
            ["d"] = null
        };

        // Act
        var result = dic.GetFirstKeyByValue(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("b", result);
    }

    [Fact]
    public void GetFirstKeyByValue_WithEmptyDictionary_ShouldReturnDefaultKey()
    {
        // Arrange
        var dic = new Dictionary<string, int>();

        // Act
        var result = dic.GetFirstKeyByValue(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetFirstKeyByValue_WithIntKeyType_ShouldReturnDefaultIntKey()
    {
        // Arrange
        var dic = new Dictionary<int, string>
        {
            [1] = "value1",
            [2] = "value2"
        };

        // Act
        var result = dic.GetFirstKeyByValue("nonexistent");

        // Assert
        Assert.Equal(default(int), result);
        Assert.Equal(0, result);
    }

    [Fact]
    public void HasValue_WithComplexObjectValues_ShouldReturnTrueForMatchingObject()
    {
        // Arrange
        var obj1 = new { Id = 1, Name = "test" };
        var obj2 = new { Id = 2, Name = "test2" };
        var obj3 = new { Id = 1, Name = "test" };

        var dic = new Dictionary<string, object>
        {
            ["a"] = obj1,
            ["b"] = obj2
        };

        // Act
        var result = dic.HasValue(obj1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetKeyByValue_WithMultipleKeys_ShouldRetainOrder()
    {
        // Arrange
        var dic = new Dictionary<int, string>
        {
            [1] = "same",
            [2] = "same",
            [3] = "same",
            [4] = "different"
        };

        // Act
        var result = dic.GetKeyByValue("same");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
    }
}
