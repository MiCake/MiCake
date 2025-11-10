using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiCake.Core.Util.PaginationData;

/// <summary>
/// Partial class containing retry and self-healing logic for HttpPaginationProvider.
/// This separation helps keep the main class focused on HTTP operations.
/// </summary>
public abstract partial class HttpPaginationProvider<TData>
{
    /// <summary>
    /// Execute the request with retry and self-healing logic
    /// </summary>
    private async Task<PaginationResponse<TData>> ExecuteWithRetryAsync(
        PaginationRequest<HttpPaginationRequest> request,
        CancellationToken cancellationToken)
    {
        int attemptNumber = 1;
        Exception? lastException = null;
        object? healingState = null;

        while (true)
        {
            try
            {
                var result = await FetchPageWithoutRetryAsync(request, cancellationToken, attemptNumber, healingState);

                // On success, return immediately
                if (result.IsSuccess)
                {
                    LogSuccessOnRetry(request, attemptNumber);
                    return result;
                }

                // If there's an error message, treat as failure that can be retried
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    lastException = new HttpRequestException(result.ErrorMessage);
                    OnHttpRequestFailed(lastException, request, attemptNumber);
                    
                    // Check if we should continue retrying
                    if (!ShouldContinueRetry(lastException, attemptNumber, request))
                    {
                        break; // Exit loop to return failure response
                    }

                    // Attempt self-healing
                    var (continueRetry, newHealingState) = await TryHealAndPrepareRetryAsync(lastException, request, attemptNumber, healingState);
                    healingState = newHealingState;
                    
                    if (!continueRetry)
                    {
                        break; // Exit loop to return failure response
                    }

                    // Wait before next attempt
                    await WaitBeforeRetryAsync(lastException, request, ++attemptNumber, cancellationToken);
                    continue;
                }

                // No data and no error - treat as retriable
                lastException = new InvalidOperationException("Request returned no data and no error");
            }
            catch (Exception ex)
            {
                lastException = ex;
                OnHttpRequestFailed(ex, request, attemptNumber);

                // Check if we should continue retrying
                if (!ShouldContinueRetry(ex, attemptNumber, request))
                {
                    break;
                }

                // Attempt self-healing
                var (continueRetry, newHealingState) = await TryHealAndPrepareRetryAsync(ex, request, attemptNumber, healingState);
                healingState = newHealingState;
                
                if (!continueRetry)
                {
                    break;
                }

                // Wait before next attempt
                await WaitBeforeRetryAsync(ex, request, ++attemptNumber, cancellationToken);
            }
        }

        return CreateFailureResponse(attemptNumber, lastException);
    }

    /// <summary>
    /// Log success message if request succeeded after retries
    /// </summary>
    private void LogSuccessOnRetry(PaginationRequest<HttpPaginationRequest> request, int attemptNumber)
    {
        if (attemptNumber > 1)
        {
            _logger.LogInformation("Request succeeded for {Identifier} at offset {Offset} on attempt {Attempt}",
                request.Identifier, request.Offset, attemptNumber);
        }
    }

    /// <summary>
    /// Check if we should continue retrying
    /// </summary>
    private bool ShouldContinueRetry(Exception exception, int attemptNumber, PaginationRequest<HttpPaginationRequest> request)
    {
        // No retry if policy is not set
        if (_retryPolicy == null)
        {
            return false;
        }

        if (!_retryPolicy.ShouldRetryException(exception, attemptNumber))
        {
            _logger.LogWarning("Exception is not retriable or max attempts reached for {Identifier}: {Exception}",
                request.Identifier, exception.Message);
            return false;
        }

        if (_retryPolicy.MaxAttempts >= 0 && attemptNumber >= _retryPolicy.MaxAttempts)
        {
            OnRetryExhausted(exception, request, attemptNumber);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempt self-healing and prepare for retry.
    /// Returns a tuple of (shouldContinueRetry, newHealingState)
    /// </summary>
    private async Task<(bool, object?)> TryHealAndPrepareRetryAsync(
        Exception exception,
        PaginationRequest<HttpPaginationRequest> request,
        int attemptNumber,
        object? healingState)
    {
        var healingContext = new SelfHealingContext
        {
            Exception = exception,
            Request = request,
            AttemptNumber = attemptNumber,
            State = healingState
        };

        var healingResult = await AttemptSelfHealingAsync(healingContext);

        if (healingResult.IsSuccessful)
        {
            _logger.LogInformation("Self-healing successful for {Identifier}: {Message}",
                request.Identifier, healingResult.Message);
        }

        if (!healingResult.ContinueRetry)
        {
            _logger.LogWarning("Self-healing requested to stop retries for {Identifier}: {Message}",
                request.Identifier, healingResult.Message);
            return (false, healingResult.State);
        }

        return (true, healingResult.State);
    }

    /// <summary>
    /// Wait before the next retry attempt
    /// </summary>
    private async Task WaitBeforeRetryAsync(
        Exception exception,
        PaginationRequest<HttpPaginationRequest> request,
        int nextAttemptNumber,
        CancellationToken cancellationToken)
    {
        if (_retryPolicy == null)
        {
            return;
        }

        var delay = _retryPolicy.CalculateDelay(nextAttemptNumber - 1);
        OnBeforeRetry(exception, request, nextAttemptNumber, delay);

        if (delay > 0)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }

    /// <summary>
    /// Create a failure response after all retries exhausted
    /// </summary>
    private PaginationResponse<TData> CreateFailureResponse(int attemptNumber, Exception? lastException)
    {
        return new PaginationResponse<TData>
        {
            Data = null,
            HasMore = false,
            ErrorMessage = $"Request failed after {attemptNumber} attempts: {lastException?.Message}"
        };
    }
}
