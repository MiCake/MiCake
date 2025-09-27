using System;

namespace MiCake.Core.Util.CircuitBreaker;

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Normal operation - requests are allowed
    /// </summary>
    Closed,
    
    /// <summary>
    /// Circuit is open - requests are blocked
    /// </summary>
    Open,
    
    /// <summary>
    /// Testing if the provider has recovered
    /// </summary>
    HalfOpen
}

/// <summary>
/// Circuit breaker state for individual providers
/// </summary>
public class CircuitBreakerState
{
    public CircuitState State { get; set; } = CircuitState.Closed;
    public int FailureCount { get; set; }
    public int SuccessiveSuccesses { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime LastTestTime { get; set; }
    public int ConcurrentOperations { get; set; }
}