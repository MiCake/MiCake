using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Util.CircuitBreaker;

/// <summary>
/// Helper class for configuring circuit breaker provider priorities
/// </summary>
public static class CircuitBreakerExtensions
{
    /// <summary>
    /// Register generic circuit breaker in DI container.
    /// </summary>
    public static IServiceCollection AddGenericCircuitBreaker(this IServiceCollection services)
    {
        services.AddTransient(typeof(GenericCircuitBreaker<,>));

        return services;
    }

    /// <summary>
    /// Configure provider with highest priority (0)
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="circuitBreaker">Circuit breaker instance</param>
    /// <param name="providerName">Provider name</param>
    /// <returns>Circuit breaker for method chaining</returns>
    public static GenericCircuitBreaker<TRequest, TResponse> WithPrimaryProvider<TRequest, TResponse>(
        this GenericCircuitBreaker<TRequest, TResponse> circuitBreaker,
        string providerName)
    {
        circuitBreaker.SetProviderPriority(providerName, 0);
        return circuitBreaker;
    }

    /// <summary>
    /// Configure provider as fallback (high priority number)
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="circuitBreaker">Circuit breaker instance</param>
    /// <param name="providerName">Provider name</param>
    /// <param name="priority">Priority level (default: 100)</param>
    /// <returns>Circuit breaker for method chaining</returns>
    public static GenericCircuitBreaker<TRequest, TResponse> WithFallbackProvider<TRequest, TResponse>(
        this GenericCircuitBreaker<TRequest, TResponse> circuitBreaker,
        string providerName,
        int priority = 100)
    {
        circuitBreaker.SetProviderPriority(providerName, priority);
        return circuitBreaker;
    }

    /// <summary>
    /// Configure multiple providers with ascending priorities
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="circuitBreaker">Circuit breaker instance</param>
    /// <param name="providerNames">Provider names in order of preference (first = highest priority)</param>
    /// <returns>Circuit breaker for method chaining</returns>
    public static GenericCircuitBreaker<TRequest, TResponse> WithProviderOrder<TRequest, TResponse>(
        this GenericCircuitBreaker<TRequest, TResponse> circuitBreaker,
        params string[] providerNames)
    {
        var priorities = providerNames
            .Select((name, index) => new { name, priority = index * 10 })
            .ToDictionary(x => x.name, x => x.priority);

        circuitBreaker.SetProviderPriorities(priorities);
        return circuitBreaker;
    }

    /// <summary>
    /// Set selection strategy with fluent interface
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="circuitBreaker">Circuit breaker instance</param>
    /// <param name="strategy">Selection strategy</param>
    /// <returns>Circuit breaker for method chaining</returns>
    public static GenericCircuitBreaker<TRequest, TResponse> WithStrategy<TRequest, TResponse>(
        this GenericCircuitBreaker<TRequest, TResponse> circuitBreaker,
        ProviderSelectionStrategy strategy)
    {
        circuitBreaker.SetSelectionStrategy(strategy);
        return circuitBreaker;
    }
}

/// <summary>
/// Builder for creating circuit breaker configurations
/// </summary>
public class CircuitBreakerConfigBuilder
{
    private readonly CircuitBreakerConfig _config = new();

    /// <summary>
    /// Set failure threshold
    /// </summary>
    /// <param name="threshold">Number of failures before opening circuit</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerConfigBuilder WithFailureThreshold(int threshold)
    {
        _config.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Set success threshold
    /// </summary>
    /// <param name="threshold">Number of successes to close circuit</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerConfigBuilder WithSuccessThreshold(int threshold)
    {
        _config.SuccessThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Set open state timeout
    /// </summary>
    /// <param name="timeout">Time to wait before retrying</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerConfigBuilder WithOpenStateTimeout(TimeSpan timeout)
    {
        _config.OpenStateTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Set maximum concurrent operations
    /// </summary>
    /// <param name="maxOperations">Maximum concurrent operations per provider</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerConfigBuilder WithMaxConcurrentOperations(int maxOperations)
    {
        _config.MaxConcurrentOperations = maxOperations;
        return this;
    }

    /// <summary>
    /// Set provider priorities
    /// </summary>
    /// <param name="priorities">Dictionary of provider name -> priority</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerConfigBuilder WithProviderPriorities(Dictionary<string, int> priorities)
    {
        _config.ProviderPriorities = new Dictionary<string, int>(priorities);
        return this;
    }

    /// <summary>
    /// Set provider order (first = highest priority)
    /// </summary>
    /// <param name="providerNames">Provider names in order of preference</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerConfigBuilder WithProviderOrder(params string[] providerNames)
    {
        var priorities = providerNames
            .Select((name, index) => new { name, priority = index * 10 })
            .ToDictionary(x => x.name, x => x.priority);

        _config.ProviderPriorities = priorities;
        return this;
    }

    /// <summary>
    /// Set selection strategy
    /// </summary>
    /// <param name="strategy">Provider selection strategy</param>
    /// <returns>Builder for method chaining</returns>
    public CircuitBreakerConfigBuilder WithSelectionStrategy(ProviderSelectionStrategy strategy)
    {
        _config.SelectionStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Build the configuration
    /// </summary>
    /// <returns>Circuit breaker configuration</returns>
    public CircuitBreakerConfig Build()
    {
        return _config;
    }
}