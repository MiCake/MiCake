using System;
using System.Collections.Generic;

namespace MiCake.Util.CircuitBreaker;

/// <summary>
/// Circuit breaker configuration
/// </summary>
public class CircuitBreakerConfig
{
    private int _failureThreshold = 3;
    private int _successThreshold = 2;
    private int _maxConcurrentOperations = 100;

    /// <summary>
    /// Number of failures before opening the circuit (default: 3)
    /// </summary>
    public int FailureThreshold 
    { 
        get => _failureThreshold; 
        set => _failureThreshold = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value), "FailureThreshold must be greater than 0") : value; 
    }

    /// <summary>
    /// Number of successes required to close the circuit from half-open state (default: 2)
    /// </summary>
    public int SuccessThreshold 
    { 
        get => _successThreshold; 
        set => _successThreshold = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value), "SuccessThreshold must be greater than 0") : value; 
    }

    /// <summary>
    /// Time to wait before attempting to close the circuit (default: 5 minutes)
    /// </summary>
    public TimeSpan OpenStateTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval for background health checks (default: 1 minute)
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum number of concurrent operations (default: 100)
    /// </summary>
    public int MaxConcurrentOperations 
    { 
        get => _maxConcurrentOperations; 
        set => _maxConcurrentOperations = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value), "MaxConcurrentOperations must be greater than 0") : value; 
    }

    /// <summary>
    /// Provider priority mapping (provider name -> priority)
    /// Lower numbers have higher priority (default: all providers have priority 0)
    /// </summary>
    public Dictionary<string, int> ProviderPriorities { get; set; } = new();

    /// <summary>
    /// Strategy for selecting providers when multiple are available
    /// </summary>
    public ProviderSelectionStrategy SelectionStrategy { get; set; } = ProviderSelectionStrategy.PriorityOrder;
}

/// <summary>
/// Strategy for selecting providers
/// </summary>
public enum ProviderSelectionStrategy
{
    /// <summary>
    /// Use providers in priority order (highest priority first)
    /// </summary>
    PriorityOrder,

    /// <summary>
    /// Use all providers in parallel and return first successful result
    /// </summary>
    ParallelRace,

    /// <summary>
    /// Round-robin between available providers
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Use provider with lowest current load (concurrent operations)
    /// </summary>
    LeastLoad
}