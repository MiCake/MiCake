using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Util.CircuitBreaker;

/// <summary>
/// Generic interface for providers that can be used with circuit breaker
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface ICircuitBreakerProvider<in TRequest, TResponse>
{
    /// <summary>
    /// Provider name for identification
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Execute the operation
    /// </summary>
    /// <param name="request">Request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    Task<TResponse?> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}