using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiCake.Core.Util.CircuitBreaker;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MiCake.Core.Tests.Util.CircuitBreaker;

public class CircuitBreakerExtensionsTests
{
    [Fact]
    public void AddGenericCircuitBreaker_ShouldRegisterService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddGenericCircuitBreaker();

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(GenericCircuitBreaker<,>));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Transient, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void WithPrimaryProvider_ShouldSetPriorityToZero()
    {
        // Arrange
        var mockProvider = new TestCircuitBreakerProvider("TestProvider");
        var providers = new[] { mockProvider };
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GenericCircuitBreaker<string, string>>();
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, mockLogger);

        // Act
        var result = circuitBreaker.WithPrimaryProvider("TestProvider");

        // Assert
        Assert.Same(circuitBreaker, result);
        var priorities = circuitBreaker.GetProviderPriorities();
        Assert.Equal(0, priorities["TestProvider"]);
    }

    [Fact]
    public void WithFallbackProvider_ShouldSetHighPriority()
    {
        // Arrange
        var mockProvider = new TestCircuitBreakerProvider("TestProvider");
        var providers = new[] { mockProvider };
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GenericCircuitBreaker<string, string>>();
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, mockLogger);

        // Act
        var result = circuitBreaker.WithFallbackProvider("TestProvider", 50);

        // Assert
        Assert.Same(circuitBreaker, result);
        var priorities = circuitBreaker.GetProviderPriorities();
        Assert.Equal(50, priorities["TestProvider"]);
    }

    [Fact]
    public void WithFallbackProvider_WithDefaultPriority_ShouldSetTo100()
    {
        // Arrange
        var mockProvider = new TestCircuitBreakerProvider("TestProvider");
        var providers = new[] { mockProvider };
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GenericCircuitBreaker<string, string>>();
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, mockLogger);

        // Act
        var result = circuitBreaker.WithFallbackProvider("TestProvider");

        // Assert
        Assert.Same(circuitBreaker, result);
        var priorities = circuitBreaker.GetProviderPriorities();
        Assert.Equal(100, priorities["TestProvider"]);
    }

    [Fact]
    public void WithProviderOrder_ShouldSetAscendingPriorities()
    {
        // Arrange
        var provider1 = new TestCircuitBreakerProvider("Provider1");
        var provider2 = new TestCircuitBreakerProvider("Provider2");
        var provider3 = new TestCircuitBreakerProvider("Provider3");
        var providers = new[] { provider1, provider2, provider3 };
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GenericCircuitBreaker<string, string>>();
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, mockLogger);

        // Act
        var result = circuitBreaker.WithProviderOrder("Provider1", "Provider2", "Provider3");

        // Assert
        Assert.Same(circuitBreaker, result);
        var priorities = circuitBreaker.GetProviderPriorities();
        Assert.Equal(0, priorities["Provider1"]);
        Assert.Equal(10, priorities["Provider2"]);
        Assert.Equal(20, priorities["Provider3"]);
    }

    [Fact]
    public void WithStrategy_ShouldSetSelectionStrategy()
    {
        // Arrange
        var mockProvider = new TestCircuitBreakerProvider("TestProvider");
        var providers = new[] { mockProvider };
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GenericCircuitBreaker<string, string>>();
        var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, mockLogger);

        // Act
        var result = circuitBreaker.WithStrategy(ProviderSelectionStrategy.RoundRobin);

        // Assert
        Assert.Same(circuitBreaker, result);
        // Strategy setting is verified through behavior in main circuit breaker tests
    }

    private class TestCircuitBreakerProvider : ICircuitBreakerProvider<string, string>
    {
        public string ProviderName { get; }

        public TestCircuitBreakerProvider(string providerName)
        {
            ProviderName = providerName;
        }

        public Task<string?> ExecuteAsync(string request, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>("test-response");
        }

        public Task<bool> IsAvailableAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}

public class CircuitBreakerConfigBuilderTests
{
    [Fact]
    public void WithFailureThreshold_ShouldSetValue()
    {
        // Arrange
        var builder = new CircuitBreakerConfigBuilder();

        // Act
        var result = builder.WithFailureThreshold(5);
        var config = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(5, config.FailureThreshold);
    }

    [Fact]
    public void WithSuccessThreshold_ShouldSetValue()
    {
        // Arrange
        var builder = new CircuitBreakerConfigBuilder();

        // Act
        var result = builder.WithSuccessThreshold(3);
        var config = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(3, config.SuccessThreshold);
    }

    [Fact]
    public void WithOpenStateTimeout_ShouldSetValue()
    {
        // Arrange
        var builder = new CircuitBreakerConfigBuilder();
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var result = builder.WithOpenStateTimeout(timeout);
        var config = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(timeout, config.OpenStateTimeout);
    }

    [Fact]
    public void WithMaxConcurrentOperations_ShouldSetValue()
    {
        // Arrange
        var builder = new CircuitBreakerConfigBuilder();

        // Act
        var result = builder.WithMaxConcurrentOperations(50);
        var config = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(50, config.MaxConcurrentOperations);
    }

    [Fact]
    public void WithProviderPriorities_ShouldSetValues()
    {
        // Arrange
        var builder = new CircuitBreakerConfigBuilder();
        var priorities = new Dictionary<string, int>
        {
            ["Provider1"] = 1,
            ["Provider2"] = 2
        };

        // Act
        var result = builder.WithProviderPriorities(priorities);
        var config = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(2, config.ProviderPriorities.Count);
        Assert.Equal(1, config.ProviderPriorities["Provider1"]);
        Assert.Equal(2, config.ProviderPriorities["Provider2"]);
    }

    [Fact]
    public void WithProviderOrder_ShouldSetAscendingPriorities()
    {
        // Arrange
        var builder = new CircuitBreakerConfigBuilder();

        // Act
        var result = builder.WithProviderOrder("Provider1", "Provider2", "Provider3");
        var config = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(3, config.ProviderPriorities.Count);
        Assert.Equal(0, config.ProviderPriorities["Provider1"]);
        Assert.Equal(10, config.ProviderPriorities["Provider2"]);
        Assert.Equal(20, config.ProviderPriorities["Provider3"]);
    }

    [Fact]
    public void WithSelectionStrategy_ShouldSetValue()
    {
        // Arrange
        var builder = new CircuitBreakerConfigBuilder();

        // Act
        var result = builder.WithSelectionStrategy(ProviderSelectionStrategy.LeastLoad);
        var config = result.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(ProviderSelectionStrategy.LeastLoad, config.SelectionStrategy);
    }

    [Fact]
    public void Build_ShouldReturnConfigWithAllSetValues()
    {
        // Arrange
        var builder = new CircuitBreakerConfigBuilder();
        var timeout = TimeSpan.FromMinutes(2);
        var priorities = new Dictionary<string, int> { ["Test"] = 5 };

        // Act
        var config = builder
            .WithFailureThreshold(7)
            .WithSuccessThreshold(4)
            .WithOpenStateTimeout(timeout)
            .WithMaxConcurrentOperations(200)
            .WithProviderPriorities(priorities)
            .WithSelectionStrategy(ProviderSelectionStrategy.ParallelRace)
            .Build();

        // Assert
        Assert.Equal(7, config.FailureThreshold);
        Assert.Equal(4, config.SuccessThreshold);
        Assert.Equal(timeout, config.OpenStateTimeout);
        Assert.Equal(200, config.MaxConcurrentOperations);
        Assert.Equal(ProviderSelectionStrategy.ParallelRace, config.SelectionStrategy);
        Assert.Equal(5, config.ProviderPriorities["Test"]);
    }
}