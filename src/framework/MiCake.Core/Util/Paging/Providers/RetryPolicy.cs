using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MiCake.Util.Paging.Providers;

/// <summary>
/// Defines the retry strategy for failed HTTP requests
/// </summary>
public enum RetryStrategy
{
    /// <summary>
    /// No retry - fail immediately on first error
    /// </summary>
    None,

    /// <summary>
    /// Fixed delay between retry attempts
    /// </summary>
    FixedDelay,

    /// <summary>
    /// Exponentially increasing delay between attempts
    /// </summary>
    ExponentialBackoff,

    /// <summary>
    /// Linear increase in delay between attempts
    /// </summary>
    LinearBackoff,

    /// <summary>
    /// Custom retry strategy - user provides their own delay calculation
    /// </summary>
    Custom
}

/// <summary>
/// Configuration for HTTP request retry behavior
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts (0 = no retries, -1 = unlimited)
    /// Default: 3
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// The retry strategy to use
    /// Default: ExponentialBackoff
    /// </summary>
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;

    /// <summary>
    /// Initial delay in milliseconds before first retry
    /// Default: 1000ms (1 second)
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum delay in milliseconds between retries
    /// Default: 30000ms (30 seconds)
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Multiplier for exponential backoff strategy
    /// Default: 2.0 (doubles each time)
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Custom delay calculator function
    /// Parameters: attemptNumber (1-based), previousDelayMs
    /// Returns: delay in milliseconds
    /// </summary>
    public Func<int, int, int>? CustomDelayCalculator { get; set; }

    /// <summary>
    /// Determines if an exception should trigger a retry
    /// Default: returns true for all exceptions
    /// </summary>
    public Func<Exception, int, bool>? ShouldRetry { get; set; }

    /// <summary>
    /// Whether to enable jitter (randomization) to prevent thundering herd
    /// Default: true
    /// </summary>
    public bool EnableJitter { get; set; } = true;

    /// <summary>
    /// Jitter factor (0.0 to 1.0) - percentage of delay that can be randomized
    /// Default: 0.2 (±20%)
    /// </summary>
    public double JitterFactor { get; set; } = 0.2;

    /// <summary>
    /// Calculate the delay for a specific retry attempt
    /// </summary>
    /// <param name="attemptNumber">Current attempt number (1-based)</param>
    /// <returns>Delay in milliseconds</returns>
    public int CalculateDelay(int attemptNumber)
    {
        if (attemptNumber < 1)
            throw new ArgumentException("Attempt number must be >= 1", nameof(attemptNumber));

        int delay = Strategy switch
        {
            RetryStrategy.None => 0,
            RetryStrategy.FixedDelay => InitialDelayMs,
            RetryStrategy.LinearBackoff => InitialDelayMs * attemptNumber,
            RetryStrategy.ExponentialBackoff => CalculateExponentialDelay(attemptNumber),
            RetryStrategy.Custom => CalculateCustomDelay(attemptNumber),
            _ => InitialDelayMs
        };

        // Apply jitter if enabled
        if (EnableJitter && delay > 0)
        {
            delay = ApplyJitter(delay);
        }

        return Math.Min(delay, MaxDelayMs);
    }

    private int CalculateExponentialDelay(int attemptNumber)
    {
        // Calculate: InitialDelay * (BackoffMultiplier ^ (attemptNumber - 1))
        var delay = InitialDelayMs * Math.Pow(BackoffMultiplier, attemptNumber - 1);
        return (int)Math.Min(delay, MaxDelayMs);
    }

    private int CalculateCustomDelay(int attemptNumber)
    {
        if (CustomDelayCalculator == null)
        {
            // Fallback to exponential if custom calculator not provided
            return CalculateExponentialDelay(attemptNumber);
        }

        var previousDelay = attemptNumber > 1 
            ? CalculateDelay(attemptNumber - 1) 
            : 0;

        return CustomDelayCalculator(attemptNumber, previousDelay);
    }

    private int ApplyJitter(int delay)
    {
        var random = new Random();
        var jitterRange = delay * JitterFactor;
        var jitterOffset = (random.NextDouble() * 2 - 1) * jitterRange; // -jitterRange to +jitterRange
        return Math.Max(0, (int)(delay + jitterOffset));
    }

    /// <summary>
    /// Determines if a retry should be attempted based on the exception and attempt number
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="attemptNumber">Current attempt number (1-based)</param>
    /// <returns>True if should retry, false otherwise</returns>
    public bool ShouldRetryException(Exception exception, int attemptNumber)
    {
        if (Strategy == RetryStrategy.None)
            return false;

        if (MaxAttempts >= 0 && attemptNumber > MaxAttempts)
            return false;

        if (ShouldRetry != null)
            return ShouldRetry(exception, attemptNumber);

        // Default: retry on most common transient errors
        return exception is HttpRequestException 
            || exception is TimeoutException
            || (exception is TaskCanceledException tce && tce.InnerException is TimeoutException);
    }

    /// <summary>
    /// Creates a retry policy with no retries
    /// </summary>
    public static RetryPolicy NoRetry() => new() { Strategy = RetryStrategy.None, MaxAttempts = 0 };

    /// <summary>
    /// Creates a retry policy with fixed delay
    /// </summary>
    public static RetryPolicy FixedDelay(int maxAttempts = 3, int delayMs = 1000) => new()
    {
        Strategy = RetryStrategy.FixedDelay,
        MaxAttempts = maxAttempts,
        InitialDelayMs = delayMs
    };

    /// <summary>
    /// Creates a retry policy with exponential backoff
    /// </summary>
    public static RetryPolicy ExponentialBackoff(int maxAttempts = 3, int initialDelayMs = 1000, double multiplier = 2.0) => new()
    {
        Strategy = RetryStrategy.ExponentialBackoff,
        MaxAttempts = maxAttempts,
        InitialDelayMs = initialDelayMs,
        BackoffMultiplier = multiplier
    };
}
