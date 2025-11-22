using System;
using MiCake.Util.Query.Paging.Providers;
using Xunit;

namespace MiCake.Core.Tests.Util.Paging.Providers;

public class PaginationConfigTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var config = new PaginationConfig();

        // Assert
        Assert.Equal(1000, config.MaxItemsPerRequest);
        Assert.Equal(0, config.MaxTotalItems);
        Assert.Equal(50, config.MaxPages);
        Assert.Equal(100, config.DelayBetweenRequests);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(5000)]
    public void MaxItemsPerRequest_ValidValues_ShouldSetCorrectly(int value)
    {
        // Arrange
        var config = new PaginationConfig();

        // Act
        config.MaxItemsPerRequest = value;

        // Assert
        Assert.Equal(value, config.MaxItemsPerRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void MaxItemsPerRequest_InvalidValues_ShouldThrowArgumentOutOfRangeException(int value)
    {
        // Arrange
        var config = new PaginationConfig();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.MaxItemsPerRequest = value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(50000)]
    public void MaxTotalItems_ValidValues_ShouldSetCorrectly(int value)
    {
        // Arrange
        var config = new PaginationConfig();

        // Act
        config.MaxTotalItems = value;

        // Assert
        Assert.Equal(value, config.MaxTotalItems);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void MaxRequests_ValidValues_ShouldSetCorrectly(int value)
    {
        // Arrange
        var config = new PaginationConfig();

        // Act
        config.MaxPages = value;

        // Assert
        Assert.Equal(value, config.MaxPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void MaxRequests_InvalidValues_ShouldThrowArgumentOutOfRangeException(int value)
    {
        // Arrange
        var config = new PaginationConfig();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.MaxPages = value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(1000)]
    public void DelayBetweenRequests_ValidValues_ShouldSetCorrectly(int value)
    {
        // Arrange
        var config = new PaginationConfig();

        // Act
        config.DelayBetweenRequests = value;

        // Assert
        Assert.Equal(value, config.DelayBetweenRequests);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void DelayBetweenRequests_InvalidValues_ShouldThrowArgumentOutOfRangeException(int value)
    {
        // Arrange
        var config = new PaginationConfig();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.DelayBetweenRequests = value);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var config = new PaginationConfig();

        // Act
        config.MaxItemsPerRequest = 500;
        config.MaxTotalItems = 10000;
        config.MaxPages = 25;
        config.DelayBetweenRequests = 200;

        // Assert
        Assert.Equal(500, config.MaxItemsPerRequest);
        Assert.Equal(10000, config.MaxTotalItems);
        Assert.Equal(25, config.MaxPages);
        Assert.Equal(200, config.DelayBetweenRequests);
    }
}