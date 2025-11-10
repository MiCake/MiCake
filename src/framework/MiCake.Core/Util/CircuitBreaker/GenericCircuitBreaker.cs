using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiCake.Core.Util.CircuitBreaker;

/// <summary>
/// Generic circuit breaker implementation that can be used with any provider
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class GenericCircuitBreaker<TRequest, TResponse>
{
    private readonly ILogger<GenericCircuitBreaker<TRequest, TResponse>> _logger;
    private readonly List<ICircuitBreakerProvider<TRequest, TResponse>> _providers;
    private readonly Dictionary<string, CircuitBreakerState> _providerStates;
    private readonly CircuitBreakerConfig _config;
    private readonly Lock _lockObject = new();
    private int _roundRobinIndex = 0;

    public GenericCircuitBreaker(
        IEnumerable<ICircuitBreakerProvider<TRequest, TResponse>> providers,
        ILogger<GenericCircuitBreaker<TRequest, TResponse>> logger,
        CircuitBreakerConfig? config = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));
        _config = config ?? new CircuitBreakerConfig();

        if (_providers.Count == 0)
            throw new ArgumentException("At least one provider must be registered", nameof(providers));

        _providerStates = _providers.ToDictionary(
            p => p.ProviderName,
            _ => new CircuitBreakerState());

        _logger.LogInformation("Generic circuit breaker initialized with {Count} providers: {Providers}",
            _providers.Count, string.Join(", ", _providers.Select(p => p.ProviderName)));
    }

    /// <summary>
    /// Execute request using circuit breaker pattern with automatic failover
    /// </summary>
    public async Task<TResponse?> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var availableProviders = GetAvailableProviders();

        if (availableProviders.Count == 0)
        {
            _logger.LogWarning("No available providers for request of type {RequestType}", typeof(TRequest).Name);
            return default;
        }

        // Apply selection strategy
        var orderedProviders = ApplySelectionStrategy(availableProviders);

        switch (_config.SelectionStrategy)
        {
            case ProviderSelectionStrategy.ParallelRace:
                return await ExecuteParallelRace(orderedProviders, request, cancellationToken).ConfigureAwait(false);

            default: // PriorityOrder, RoundRobin, LeastLoad all use sequential execution
                return await ExecuteSequential(orderedProviders, request, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Get current status of all providers
    /// </summary>
    public Dictionary<string, (CircuitState State, int Failures, int Successes, int Concurrent)> GetProvidersStatus()
    {
        lock (_lockObject)
        {
            return _providerStates.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.State, kvp.Value.FailureCount, kvp.Value.SuccessiveSuccesses, kvp.Value.ConcurrentOperations));
        }
    }

    /// <summary>
    /// Force refresh provider status
    /// </summary>
    public async Task RefreshProviderStatusAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _providers.Select(async provider =>
        {
            try
            {
                var isAvailable = await provider.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
                UpdateProviderAvailability(provider.ProviderName, isAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for provider {ProviderName}", provider.ProviderName);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Set priority for a specific provider
    /// </summary>
    /// <param name="providerName">Provider name</param>
    /// <param name="priority">Priority (lower number = higher priority)</param>
    public void SetProviderPriority(string providerName, int priority)
    {
        if (!_providerStates.ContainsKey(providerName))
        {
            throw new ArgumentException($"Provider '{providerName}' not found", nameof(providerName));
        }

        _config.ProviderPriorities[providerName] = priority;
        _logger.LogInformation("Set priority {Priority} for provider {ProviderName}", priority, providerName);
    }

    /// <summary>
    /// Set priorities for multiple providers
    /// </summary>
    /// <param name="priorities">Dictionary of provider name -> priority</param>
    public void SetProviderPriorities(Dictionary<string, int> priorities)
    {
        foreach (var kvp in priorities)
        {
            if (!_providerStates.ContainsKey(kvp.Key))
            {
                _logger.LogWarning("Provider '{ProviderName}' not found, skipping priority setting", kvp.Key);
                continue;
            }

            _config.ProviderPriorities[kvp.Key] = kvp.Value;
        }

        _logger.LogInformation("Updated priorities for providers: {Priorities}",
            string.Join(", ", priorities.Select(p => $"{p.Key}:{p.Value}")));
    }

    /// <summary>
    /// Get current provider priorities
    /// </summary>
    /// <returns>Dictionary of provider name -> priority</returns>
    public Dictionary<string, int> GetProviderPriorities()
    {
        return new Dictionary<string, int>(_config.ProviderPriorities);
    }

    /// <summary>
    /// Set provider selection strategy
    /// </summary>
    /// <param name="strategy">Selection strategy</param>
    public void SetSelectionStrategy(ProviderSelectionStrategy strategy)
    {
        _config.SelectionStrategy = strategy;
        _logger.LogInformation("Changed provider selection strategy to {Strategy}", strategy);
    }

    #region Private Methods

    private List<ICircuitBreakerProvider<TRequest, TResponse>> ApplySelectionStrategy(
        List<ICircuitBreakerProvider<TRequest, TResponse>> availableProviders)
    {
        return _config.SelectionStrategy switch
        {
            ProviderSelectionStrategy.PriorityOrder => OrderByPriority(availableProviders),
            ProviderSelectionStrategy.RoundRobin => OrderByRoundRobin(availableProviders),
            ProviderSelectionStrategy.LeastLoad => OrderByLeastLoad(availableProviders),
            ProviderSelectionStrategy.ParallelRace => availableProviders, // Order doesn't matter for parallel
            _ => OrderByPriority(availableProviders)
        };
    }

    private List<ICircuitBreakerProvider<TRequest, TResponse>> OrderByPriority(
        List<ICircuitBreakerProvider<TRequest, TResponse>> providers)
    {
        return providers
            .OrderBy(p => _config.ProviderPriorities.GetValueOrDefault(p.ProviderName, 0))
            .ThenBy(p => p.ProviderName) // Secondary sort for consistency
            .ToList();
    }

    private List<ICircuitBreakerProvider<TRequest, TResponse>> OrderByRoundRobin(
        List<ICircuitBreakerProvider<TRequest, TResponse>> providers)
    {
        lock (_lockObject)
        {
            var orderedProviders = new List<ICircuitBreakerProvider<TRequest, TResponse>>();
            var providerCount = providers.Count;

            for (int i = 0; i < providerCount; i++)
            {
                var index = (_roundRobinIndex + i) % providerCount;
                orderedProviders.Add(providers[index]);
            }

            _roundRobinIndex = (_roundRobinIndex + 1) % providerCount;
            return orderedProviders;
        }
    }

    private List<ICircuitBreakerProvider<TRequest, TResponse>> OrderByLeastLoad(
        List<ICircuitBreakerProvider<TRequest, TResponse>> providers)
    {
        lock (_lockObject)
        {
            return providers
                .OrderBy(p => _providerStates[p.ProviderName].ConcurrentOperations)
                .ThenBy(p => _config.ProviderPriorities.GetValueOrDefault(p.ProviderName, 0))
                .ThenBy(p => p.ProviderName)
                .ToList();
        }
    }

    private async Task<TResponse?> ExecuteSequential(
        List<ICircuitBreakerProvider<TRequest, TResponse>> providers,
        TRequest request,
        CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            try
            {
                if (!CanExecute(provider.ProviderName))
                    continue;

                IncrementConcurrentOperations(provider.ProviderName);

                try
                {
                    var result = await provider.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

                    if (IsSuccessfulResponse(result))
                    {
                        RecordSuccess(provider.ProviderName);
                        return result;
                    }

                    _logger.LogDebug("Provider {ProviderName} returned unsuccessful response", provider.ProviderName);
                }
                finally
                {
                    DecrementConcurrentOperations(provider.ProviderName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {ProviderName} failed: {Message}",
                    provider.ProviderName, ex.Message);

                RecordFailure(provider.ProviderName);
            }
        }

        _logger.LogWarning("All providers failed for request of type {RequestType}", typeof(TRequest).Name);
        return default;
    }

    private async Task<TResponse?> ExecuteParallelRace(
        List<ICircuitBreakerProvider<TRequest, TResponse>> providers,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var executableProviders = providers.Where(p => CanExecute(p.ProviderName)).ToList();

        if (executableProviders.Count == 0)
        {
            _logger.LogWarning("No executable providers available for parallel execution");
            return default;
        }

        foreach (var provider in executableProviders)
        {
            IncrementConcurrentOperations(provider.ProviderName);
        }

        try
        {
            var tasks = executableProviders.Select(async provider =>
            {
                try
                {
                    var result = await provider.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

                    if (IsSuccessfulResponse(result))
                    {
                        RecordSuccess(provider.ProviderName);
                        return (provider.ProviderName, result, true);
                    }

                    _logger.LogDebug("Provider {ProviderName} returned unsuccessful response in parallel race",
                        provider.ProviderName);
                    return (provider.ProviderName, default(TResponse), false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderName} failed in parallel race: {Message}",
                        provider.ProviderName, ex.Message);

                    RecordFailure(provider.ProviderName);
                    return (provider.ProviderName, default(TResponse), false);
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Return first successful result, ordered by priority
            var successfulResults = results
                .Where(r => r.Item3) // Item3 is the success boolean
                .OrderBy(r => _config.ProviderPriorities.GetValueOrDefault(r.Item1, 0)); // Item1 is provider name

            var firstSuccess = successfulResults.FirstOrDefault();
            if (firstSuccess.Item3) // Check if it's successful
            {
                _logger.LogInformation("Parallel race won by provider {ProviderName}", firstSuccess.Item1);
                return firstSuccess.Item2; // Item2 is the result
            }

            _logger.LogWarning("All providers failed in parallel race for request of type {RequestType}",
                typeof(TRequest).Name);
            return default;
        }
        finally
        {
            foreach (var provider in executableProviders)
            {
                DecrementConcurrentOperations(provider.ProviderName);
            }
        }
    }

    private List<ICircuitBreakerProvider<TRequest, TResponse>> GetAvailableProviders()
    {
        var availableProviders = new List<ICircuitBreakerProvider<TRequest, TResponse>>();
        var now = DateTime.UtcNow;

        lock (_lockObject)
        {
            foreach (var provider in _providers)
            {
                var state = _providerStates[provider.ProviderName];

                if (IsProviderAvailable(state, now))
                {
                    if (state.State == CircuitState.Open)
                    {
                        state.State = CircuitState.HalfOpen;
                        state.SuccessiveSuccesses = 0;
                        _logger.LogInformation("Provider {ProviderName} moved to HalfOpen state", provider.ProviderName);
                    }
                    availableProviders.Add(provider);
                }
            }
        }

        return availableProviders;
    }

    private bool IsProviderAvailable(CircuitBreakerState state, DateTime now)
    {
        return state.State switch
        {
            CircuitState.Closed or CircuitState.HalfOpen => true,
            CircuitState.Open => (now - state.LastFailureTime) > _config.OpenStateTimeout,
            _ => false
        };
    }

    private bool CanExecute(string providerName)
    {
        lock (_lockObject)
        {
            var state = _providerStates[providerName];
            return state.ConcurrentOperations < _config.MaxConcurrentOperations;
        }
    }

    private void IncrementConcurrentOperations(string providerName)
    {
        lock (_lockObject)
        {
            _providerStates[providerName].ConcurrentOperations++;
        }
    }

    private void DecrementConcurrentOperations(string providerName)
    {
        lock (_lockObject)
        {
            var state = _providerStates[providerName];
            state.ConcurrentOperations = Math.Max(0, state.ConcurrentOperations - 1);
        }
    }

    private void RecordSuccess(string providerName)
    {
        lock (_lockObject)
        {
            var state = _providerStates[providerName];
            state.SuccessiveSuccesses++;
            state.FailureCount = Math.Max(0, state.FailureCount - 1);

            if (state.State == CircuitState.HalfOpen && state.SuccessiveSuccesses >= _config.SuccessThreshold)
            {
                state.State = CircuitState.Closed;
                state.FailureCount = 0;
                _logger.LogInformation("Provider {ProviderName} circuit closed after {Successes} successes",
                    providerName, state.SuccessiveSuccesses);
            }
        }
    }

    private void RecordFailure(string providerName)
    {
        lock (_lockObject)
        {
            var state = _providerStates[providerName];
            state.FailureCount++;
            state.SuccessiveSuccesses = 0;
            state.LastFailureTime = DateTime.UtcNow;

            if (state.FailureCount >= _config.FailureThreshold)
            {
                state.State = CircuitState.Open;
                _logger.LogWarning("Provider {ProviderName} circuit opened after {Failures} failures",
                    providerName, state.FailureCount);
            }
        }
    }

    private void UpdateProviderAvailability(string providerName, bool isAvailable)
    {
        lock (_lockObject)
        {
            var state = _providerStates[providerName];

            if (isAvailable && state.State == CircuitState.Open)
            {
                state.State = CircuitState.HalfOpen;
                state.SuccessiveSuccesses = 0;
                _logger.LogInformation("Provider {ProviderName} became available, moved to HalfOpen", providerName);
            }
            else if (!isAvailable)
            {
                state.FailureCount++;
                if (state.FailureCount >= _config.FailureThreshold)
                {
                    state.State = CircuitState.Open;
                    state.LastFailureTime = DateTime.UtcNow;
                }
            }
        }
    }

    /// <summary>
    /// Override this method to define what constitutes a successful response
    /// </summary>
    protected virtual bool IsSuccessfulResponse(TResponse? response)
    {
        return response != null;
    }

    #endregion
}