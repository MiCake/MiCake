using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiCake.Util.Query.Paging.Providers;

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
                var result = await FetchPageWithoutRetryAsync(request, cancellationToken, attemptNumber, healingState).ConfigureAwait(false);

                // On success, return immediately
                if (result.IsSuccess)
                {
                    LogSuccessOnRetry(request, attemptNumber);
                    return result;
                }

                // Handle error response
                var errorHandlingResult = await HandleErrorResponseAsync(result, request, attemptNumber, healingState, cancellationToken).ConfigureAwait(false);
                if (errorHandlingResult.ShouldBreak)
                {
                    lastException = errorHandlingResult.Exception;
                    break;
                }

                healingState = errorHandlingResult.HealingState;
                attemptNumber = errorHandlingResult.NextAttemptNumber;
                lastException = errorHandlingResult.Exception;
            }
            catch (Exception ex)
            {
                var exceptionHandlingResult = await HandleExceptionAsync(ex, request, attemptNumber, healingState, cancellationToken).ConfigureAwait(false);
                if (exceptionHandlingResult.ShouldBreak)
                {
                    lastException = ex;
                    break;
                }

                healingState = exceptionHandlingResult.HealingState;
                attemptNumber = exceptionHandlingResult.NextAttemptNumber;
            }
        }

        return HttpPaginationProvider<TData>.CreateFailureResponse(attemptNumber, lastException);
    }

    /// <summary>
    /// Handle error response from a successful HTTP call
    /// </summary>
    private async Task<RetryLoopResult> HandleErrorResponseAsync(
        PaginationResponse<TData> result,
        PaginationRequest<HttpPaginationRequest> request,
        int attemptNumber,
        object? healingState,
        CancellationToken cancellationToken)
    {
        // If there's an error message, treat as failure that can be retried
        if (string.IsNullOrEmpty(result.ErrorMessage))
        {
            // No data and no error - treat as retriable
            return new RetryLoopResult
            {
                ShouldBreak = true,
                Exception = new InvalidOperationException("Request returned no data and no error")
            };
        }

        var exception = new HttpRequestException(result.ErrorMessage);
        OnHttpRequestFailed(exception, request, attemptNumber);

        if (!ShouldContinueRetry(exception, attemptNumber, request))
        {
            return new RetryLoopResult { ShouldBreak = true, Exception = exception };
        }

        var (continueRetry, newHealingState) = await TryHealAndPrepareRetryAsync(exception, request, attemptNumber, healingState).ConfigureAwait(false);
        
        if (!continueRetry)
        {
            return new RetryLoopResult { ShouldBreak = true, Exception = exception, HealingState = newHealingState };
        }

        await WaitBeforeRetryAsync(exception, request, attemptNumber + 1, cancellationToken).ConfigureAwait(false);
        
        return new RetryLoopResult
        {
            ShouldBreak = false,
            Exception = exception,
            HealingState = newHealingState,
            NextAttemptNumber = attemptNumber + 1
        };
    }

    /// <summary>
    /// Handle exception thrown during HTTP request
    /// </summary>
    private async Task<RetryLoopResult> HandleExceptionAsync(
        Exception exception,
        PaginationRequest<HttpPaginationRequest> request,
        int attemptNumber,
        object? healingState,
        CancellationToken cancellationToken)
    {
        OnHttpRequestFailed(exception, request, attemptNumber);

        if (!ShouldContinueRetry(exception, attemptNumber, request))
        {
            return new RetryLoopResult { ShouldBreak = true };
        }

        var (continueRetry, newHealingState) = await TryHealAndPrepareRetryAsync(exception, request, attemptNumber, healingState).ConfigureAwait(false);
        
        if (!continueRetry)
        {
            return new RetryLoopResult { ShouldBreak = true, HealingState = newHealingState };
        }

        await WaitBeforeRetryAsync(exception, request, attemptNumber + 1, cancellationToken).ConfigureAwait(false);
        
        return new RetryLoopResult
        {
            ShouldBreak = false,
            HealingState = newHealingState,
            NextAttemptNumber = attemptNumber + 1
        };
    }

    /// <summary>
    /// Result of retry loop iteration
    /// </summary>
    private struct RetryLoopResult
    {
        public bool ShouldBreak { get; init; }
        public Exception? Exception { get; init; }
        public object? HealingState { get; init; }
        public int NextAttemptNumber { get; init; }
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

        var healingResult = await AttemptSelfHealingAsync(healingContext).ConfigureAwait(false);

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
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Create a failure response after all retries exhausted
    /// </summary>
    private static PaginationResponse<TData> CreateFailureResponse(int attemptNumber, Exception? lastException)
    {
        return new PaginationResponse<TData>
        {
            Data = null,
            HasMore = false,
            ErrorMessage = $"Request failed after {attemptNumber} attempts: {lastException?.Message}"
        };
    }
}
