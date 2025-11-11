using System;
using MiCake.Util.CircuitBreaker;
using Xunit;

namespace MiCake.Core.Tests.Util.CircuitBreaker;

public class CircuitBreakerStateTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var state = new CircuitBreakerState();

        // Assert
        Assert.Equal(CircuitState.Closed, state.State);
        Assert.Equal(0, state.FailureCount);
        Assert.Equal(0, state.SuccessiveSuccesses);
        Assert.Equal(default(DateTime), state.LastFailureTime);
        Assert.Equal(default(DateTime), state.LastTestTime);
        Assert.Equal(0, state.ConcurrentOperations);
    }

    [Fact]
    public void State_ShouldAllowAllValidValues()
    {
        // Arrange
        var state = new CircuitBreakerState();

        // Act & Assert
        state.State = CircuitState.Closed;
        Assert.Equal(CircuitState.Closed, state.State);

        state.State = CircuitState.Open;
        Assert.Equal(CircuitState.Open, state.State);

        state.State = CircuitState.HalfOpen;
        Assert.Equal(CircuitState.HalfOpen, state.State);
    }

    [Fact]
    public void FailureCount_ShouldAllowPositiveValues()
    {
        // Arrange
        var state = new CircuitBreakerState();

        // Act
        state.FailureCount = 5;

        // Assert
        Assert.Equal(5, state.FailureCount);
    }

    [Fact]
    public void SuccessiveSuccesses_ShouldAllowPositiveValues()
    {
        // Arrange
        var state = new CircuitBreakerState();

        // Act
        state.SuccessiveSuccesses = 3;

        // Assert
        Assert.Equal(3, state.SuccessiveSuccesses);
    }

    [Fact]
    public void LastFailureTime_ShouldAllowDateTimeValues()
    {
        // Arrange
        var state = new CircuitBreakerState();
        var now = DateTime.UtcNow;

        // Act
        state.LastFailureTime = now;

        // Assert
        Assert.Equal(now, state.LastFailureTime);
    }

    [Fact]
    public void LastTestTime_ShouldAllowDateTimeValues()
    {
        // Arrange
        var state = new CircuitBreakerState();
        var now = DateTime.UtcNow;

        // Act
        state.LastTestTime = now;

        // Assert
        Assert.Equal(now, state.LastTestTime);
    }

    [Fact]
    public void ConcurrentOperations_ShouldAllowNonNegativeValues()
    {
        // Arrange
        var state = new CircuitBreakerState();

        // Act
        state.ConcurrentOperations = 10;

        // Assert
        Assert.Equal(10, state.ConcurrentOperations);
    }

    [Fact]
    public void StateTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        var state = new CircuitBreakerState();

        // Act & Assert - Start in Closed state
        Assert.Equal(CircuitState.Closed, state.State);

        // Transition to Open
        state.State = CircuitState.Open;
        state.FailureCount = 3;
        state.LastFailureTime = DateTime.UtcNow;
        Assert.Equal(CircuitState.Open, state.State);

        // Transition to HalfOpen
        state.State = CircuitState.HalfOpen;
        state.SuccessiveSuccesses = 0;
        Assert.Equal(CircuitState.HalfOpen, state.State);

        // Transition back to Closed
        state.State = CircuitState.Closed;
        state.FailureCount = 0;
        state.SuccessiveSuccesses = 2;
        Assert.Equal(CircuitState.Closed, state.State);
    }
}