using System;
using System.Collections.Generic;
using MiCake.Core.Util.CircuitBreaker;
using Xunit;

namespace MiCake.Core.Tests.Util.CircuitBreaker;

public class CircuitBreakerConfigTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var config = new CircuitBreakerConfig();

        // Assert
        Assert.Equal(3, config.FailureThreshold);
        Assert.Equal(2, config.SuccessThreshold);
        Assert.Equal(TimeSpan.FromMinutes(5), config.OpenStateTimeout);
        Assert.Equal(TimeSpan.FromMinutes(1), config.HealthCheckInterval);
        Assert.Equal(100, config.MaxConcurrentOperations);
        Assert.NotNull(config.ProviderPriorities);
        Assert.Empty(config.ProviderPriorities);
        Assert.Equal(ProviderSelectionStrategy.PriorityOrder, config.SelectionStrategy);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void FailureThreshold_ValidValues_ShouldSetCorrectly(int value)
    {
        // Arrange
        var config = new CircuitBreakerConfig();

        // Act
        config.FailureThreshold = value;

        // Assert
        Assert.Equal(value, config.FailureThreshold);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void FailureThreshold_InvalidValues_ShouldThrowArgumentOutOfRangeException(int value)
    {
        // Arrange
        var config = new CircuitBreakerConfig();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.FailureThreshold = value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void SuccessThreshold_ValidValues_ShouldSetCorrectly(int value)
    {
        // Arrange
        var config = new CircuitBreakerConfig();

        // Act
        config.SuccessThreshold = value;

        // Assert
        Assert.Equal(value, config.SuccessThreshold);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void SuccessThreshold_InvalidValues_ShouldThrowArgumentOutOfRangeException(int value)
    {
        // Arrange
        var config = new CircuitBreakerConfig();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.SuccessThreshold = value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(1000)]
    public void MaxConcurrentOperations_ValidValues_ShouldSetCorrectly(int value)
    {
        // Arrange
        var config = new CircuitBreakerConfig();

        // Act
        config.MaxConcurrentOperations = value;

        // Assert
        Assert.Equal(value, config.MaxConcurrentOperations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void MaxConcurrentOperations_InvalidValues_ShouldThrowArgumentOutOfRangeException(int value)
    {
        // Arrange
        var config = new CircuitBreakerConfig();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.MaxConcurrentOperations = value);
    }

    [Fact]
    public void OpenStateTimeout_ShouldAllowValidTimeSpan()
    {
        // Arrange
        var config = new CircuitBreakerConfig();
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        config.OpenStateTimeout = timeout;

        // Assert
        Assert.Equal(timeout, config.OpenStateTimeout);
    }

    [Fact]
    public void HealthCheckInterval_ShouldAllowValidTimeSpan()
    {
        // Arrange
        var config = new CircuitBreakerConfig();
        var interval = TimeSpan.FromSeconds(10);

        // Act
        config.HealthCheckInterval = interval;

        // Assert
        Assert.Equal(interval, config.HealthCheckInterval);
    }

    [Fact]
    public void ProviderPriorities_ShouldAllowModification()
    {
        // Arrange
        var config = new CircuitBreakerConfig();
        var priorities = new Dictionary<string, int>
        {
            ["Provider1"] = 1,
            ["Provider2"] = 2
        };

        // Act
        config.ProviderPriorities = priorities;

        // Assert
        Assert.Equal(priorities, config.ProviderPriorities);
    }

    [Theory]
    [InlineData(ProviderSelectionStrategy.PriorityOrder)]
    [InlineData(ProviderSelectionStrategy.ParallelRace)]
    [InlineData(ProviderSelectionStrategy.RoundRobin)]
    [InlineData(ProviderSelectionStrategy.LeastLoad)]
    public void SelectionStrategy_ShouldSetCorrectly(ProviderSelectionStrategy strategy)
    {
        // Arrange
        var config = new CircuitBreakerConfig();

        // Act
        config.SelectionStrategy = strategy;

        // Assert
        Assert.Equal(strategy, config.SelectionStrategy);
    }
}