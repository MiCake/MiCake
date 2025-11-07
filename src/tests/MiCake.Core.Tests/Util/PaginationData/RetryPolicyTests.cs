using System;
using System.Net.Http;
using MiCake.Core.Util.PaginationData;
using Xunit;

namespace MiCake.Core.Tests.Util.PaginationData;

public class RetryPolicyTests
{
    [Fact]
    public void NoRetry_ShouldReturnZeroDelay()
    {
        var policy = RetryPolicy.NoRetry();
        
        Assert.Equal(RetryStrategy.None, policy.Strategy);
        Assert.Equal(0, policy.MaxAttempts);
        Assert.Equal(0, policy.CalculateDelay(1));
    }

    [Fact]
    public void FixedDelay_ShouldReturnConstantDelay()
    {
        var policy = RetryPolicy.FixedDelay(maxAttempts: 3, delayMs: 1000);
        
        // All attempts should have the same delay (approximately, due to jitter)
        policy.EnableJitter = false; // Disable jitter for predictable testing
        
        Assert.Equal(1000, policy.CalculateDelay(1));
        Assert.Equal(1000, policy.CalculateDelay(2));
        Assert.Equal(1000, policy.CalculateDelay(3));
    }

    [Fact]
    public void ExponentialBackoff_ShouldIncreaseExponentially()
    {
        var policy = RetryPolicy.ExponentialBackoff(maxAttempts: 3, initialDelayMs: 1000, multiplier: 2.0);
        policy.EnableJitter = false; // Disable jitter for predictable testing
        
        Assert.Equal(1000, policy.CalculateDelay(1));   // 1000 * 2^0
        Assert.Equal(2000, policy.CalculateDelay(2));   // 1000 * 2^1
        Assert.Equal(4000, policy.CalculateDelay(3));   // 1000 * 2^2
    }

    [Fact]
    public void LinearBackoff_ShouldIncreaseLinearly()
    {
        var policy = new RetryPolicy
        {
            Strategy = RetryStrategy.LinearBackoff,
            InitialDelayMs = 1000,
            MaxAttempts = 3,
            EnableJitter = false
        };
        
        Assert.Equal(1000, policy.CalculateDelay(1));   // 1000 * 1
        Assert.Equal(2000, policy.CalculateDelay(2));   // 1000 * 2
        Assert.Equal(3000, policy.CalculateDelay(3));   // 1000 * 3
    }

    [Fact]
    public void CalculateDelay_ShouldRespectMaxDelay()
    {
        var policy = new RetryPolicy
        {
            Strategy = RetryStrategy.ExponentialBackoff,
            InitialDelayMs = 1000,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 5000,
            MaxAttempts = 10,
            EnableJitter = false
        };
        
        // After a few attempts, delay should cap at MaxDelayMs
        Assert.Equal(1000, policy.CalculateDelay(1));
        Assert.Equal(2000, policy.CalculateDelay(2));
        Assert.Equal(4000, policy.CalculateDelay(3));
        Assert.Equal(5000, policy.CalculateDelay(4)); // Capped
        Assert.Equal(5000, policy.CalculateDelay(5)); // Still capped
    }

    [Fact]
    public void Jitter_ShouldRandomizeDelay()
    {
        var policy = new RetryPolicy
        {
            Strategy = RetryStrategy.FixedDelay,
            InitialDelayMs = 1000,
            EnableJitter = true,
            JitterFactor = 0.2 // ±20%
        };

        // Calculate multiple delays and ensure they vary
        var delays = new int[10];
        for (int i = 0; i < 10; i++)
        {
            delays[i] = policy.CalculateDelay(1);
        }

        // At least some delays should be different (very unlikely all are the same with jitter)
        var allSame = true;
        for (int i = 1; i < delays.Length; i++)
        {
            if (delays[i] != delays[0])
            {
                allSame = false;
                break;
            }
        }
        
        Assert.False(allSame, "Jitter should produce varying delays");
        
        // All delays should be within ±20% of 1000ms
        foreach (var delay in delays)
        {
            Assert.InRange(delay, 800, 1200);
        }
    }

    [Fact]
    public void CustomDelayCalculator_ShouldBeUsed()
    {
        var policy = new RetryPolicy
        {
            Strategy = RetryStrategy.Custom,
            CustomDelayCalculator = (attempt, previousDelay) => attempt * 500,
            EnableJitter = false
        };
        
        Assert.Equal(500, policy.CalculateDelay(1));
        Assert.Equal(1000, policy.CalculateDelay(2));
        Assert.Equal(1500, policy.CalculateDelay(3));
    }

    [Fact]
    public void ShouldRetryException_HttpRequestException_ShouldRetry()
    {
        var policy = new RetryPolicy { MaxAttempts = 3 };
        var exception = new HttpRequestException("Network error");
        
        Assert.True(policy.ShouldRetryException(exception, 1));
        Assert.True(policy.ShouldRetryException(exception, 2));
        Assert.True(policy.ShouldRetryException(exception, 3));
        Assert.False(policy.ShouldRetryException(exception, 4)); // Exceeds max attempts
    }

    [Fact]
    public void ShouldRetryException_TimeoutException_ShouldRetry()
    {
        var policy = new RetryPolicy { MaxAttempts = 3 };
        var exception = new TimeoutException("Request timeout");
        
        Assert.True(policy.ShouldRetryException(exception, 1));
    }

    [Fact]
    public void ShouldRetryException_CustomPredicate_ShouldBeUsed()
    {
        var policy = new RetryPolicy
        {
            MaxAttempts = 5,
            ShouldRetry = (ex, attempt) => ex is InvalidOperationException && attempt <= 2
        };
        
        var retryableEx = new InvalidOperationException();
        var nonRetryableEx = new ArgumentException();
        
        Assert.True(policy.ShouldRetryException(retryableEx, 1));
        Assert.True(policy.ShouldRetryException(retryableEx, 2));
        Assert.False(policy.ShouldRetryException(retryableEx, 3)); // Custom predicate returns false
        Assert.False(policy.ShouldRetryException(nonRetryableEx, 1));
    }

    [Fact]
    public void ShouldRetryException_NoRetryStrategy_ShouldNotRetry()
    {
        var policy = RetryPolicy.NoRetry();
        var exception = new HttpRequestException();
        
        Assert.False(policy.ShouldRetryException(exception, 1));
    }

    [Fact]
    public void CalculateDelay_InvalidAttemptNumber_ShouldThrow()
    {
        var policy = new RetryPolicy();
        
        Assert.Throws<ArgumentException>(() => policy.CalculateDelay(0));
        Assert.Throws<ArgumentException>(() => policy.CalculateDelay(-1));
    }
}
