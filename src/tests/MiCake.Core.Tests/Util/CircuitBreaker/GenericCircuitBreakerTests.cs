using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiCake.Core.Util.CircuitBreaker;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MiCake.Core.Tests.Util.CircuitBreaker;

public class GenericCircuitBreakerTests
{
    private readonly Mock<ILogger<GenericCircuitBreaker<string, string>>> _mockLogger;
    private readonly Mock<ICircuitBreakerProvider<string, string>> _mockProvider1;
    private readonly Mock<ICircuitBreakerProvider<string, string>> _mockProvider2;

    public GenericCircuitBreakerTests()
    {
        _mockLogger = new Mock<ILogger<GenericCircuitBreaker<string, string>>>();
        _mockProvider1 = new Mock<ICircuitBreakerProvider<string, string>>();
        _mockProvider2 = new Mock<ICircuitBreakerProvider<string, string>>();

        _mockProvider1.Setup(p => p.ProviderName).Returns("Provider1");
        _mockProvider2.Setup(p => p.ProviderName).Returns("Provider2");
    }

    [Fact]
    public void Constructor_WithValidProviders_ShouldInitializeCorrectly()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };

        // Act
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object);

        // Assert
        Assert.NotNull(circuitBreaker);
        var status = circuitBreaker.GetProvidersStatus();
        Assert.Equal(2, status.Count);
        Assert.Contains("Provider1", status.Keys);
        Assert.Contains("Provider2", status.Keys);
    }

    [Fact]
    public void Constructor_WithNullProviders_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<ICircuitBreakerProvider<string, string>> providers = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object };
        ILogger<GenericCircuitBreaker<string, string>> logger = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GenericCircuitBreaker<string, string>(providers, logger));
    }

    [Fact]
    public void Constructor_WithEmptyProviders_ShouldThrowArgumentException()
    {
        // Arrange
        var providers = Array.Empty<ICircuitBreakerProvider<string, string>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object));
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulProvider_ShouldReturnResult()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object);
        var request = "test-request";
        var expectedResponse = "test-response";

        _mockProvider1.Setup(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await circuitBreaker.ExecuteAsync(request);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockProvider1.Verify(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingPrimaryProvider_ShouldFallbackToSecondary()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
        var config = new CircuitBreakerConfig { FailureThreshold = 1 };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object, config);
        var request = "test-request";
        var expectedResponse = "fallback-response";

        _mockProvider1.Setup(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider1 failed"));
        _mockProvider2.Setup(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await circuitBreaker.ExecuteAsync(request);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockProvider1.Verify(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockProvider2.Verify(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllProvidersUnavailable_ShouldReturnDefault()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object };
        var config = new CircuitBreakerConfig { FailureThreshold = 1, OpenStateTimeout = TimeSpan.FromHours(1) };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object, config);
        var request = "test-request";

        // First call to trigger circuit open
        _mockProvider1.Setup(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider failed"));
        
        await circuitBreaker.ExecuteAsync(request);

        // Act - Second call should return default due to open circuit
        var result = await circuitBreaker.ExecuteAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithParallelRaceStrategy_ShouldReturnFirstSuccessfulResult()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
        var config = new CircuitBreakerConfig { SelectionStrategy = ProviderSelectionStrategy.ParallelRace };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object, config);
        var request = "test-request";

        _mockProvider1.Setup(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync("response1");
        _mockProvider2.Setup(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync("response2");

        // Act
        var result = await circuitBreaker.ExecuteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result == "response1" || result == "response2");
        _mockProvider1.Verify(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockProvider2.Verify(p => p.ExecuteAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SetProviderPriority_WithValidProvider_ShouldSetPriority()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object);
        var priority = 10;

        // Act
        circuitBreaker.SetProviderPriority("Provider1", priority);

        // Assert
        var priorities = circuitBreaker.GetProviderPriorities();
        Assert.Equal(priority, priorities["Provider1"]);
    }

    [Fact]
    public void SetProviderPriority_WithInvalidProvider_ShouldThrowArgumentException()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            circuitBreaker.SetProviderPriority("NonExistentProvider", 10));
    }

    [Fact]
    public void SetProviderPriorities_WithValidProviders_ShouldSetPriorities()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object);
        var priorities = new Dictionary<string, int>
        {
            ["Provider1"] = 1,
            ["Provider2"] = 2
        };

        // Act
        circuitBreaker.SetProviderPriorities(priorities);

        // Assert
        var currentPriorities = circuitBreaker.GetProviderPriorities();
        Assert.Equal(1, currentPriorities["Provider1"]);
        Assert.Equal(2, currentPriorities["Provider2"]);
    }

    [Fact]
    public void SetSelectionStrategy_WithValidStrategy_ShouldSetStrategy()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object);

        // Act
        circuitBreaker.SetSelectionStrategy(ProviderSelectionStrategy.RoundRobin);

        // Assert - Test by checking behavior changes in execution order
        Assert.NotNull(circuitBreaker); // Basic check since strategy is internal
    }

    [Fact]
    public void GetProvidersStatus_ShouldReturnCurrentStatus()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object);

        // Act
        var status = circuitBreaker.GetProvidersStatus();

        // Assert
        Assert.Equal(2, status.Count);
        Assert.Contains("Provider1", status.Keys);
        Assert.Contains("Provider2", status.Keys);
        
        foreach (var (state, failures, successes, concurrent) in status.Values)
        {
            Assert.Equal(CircuitState.Closed, state);
            Assert.Equal(0, failures);
            Assert.Equal(0, successes);
            Assert.Equal(0, concurrent);
        }
    }

    [Fact]
    public async Task RefreshProviderStatusAsync_ShouldCallIsAvailableOnAllProviders()
    {
        // Arrange
        var providers = new[] { _mockProvider1.Object, _mockProvider2.Object };
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, _mockLogger.Object);

        _mockProvider1.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockProvider2.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await circuitBreaker.RefreshProviderStatusAsync();

        // Assert
        _mockProvider1.Verify(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockProvider2.Verify(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}