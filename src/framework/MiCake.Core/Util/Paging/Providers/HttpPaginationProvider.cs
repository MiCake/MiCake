using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiCake.Util.Paging.Providers;

/// <summary>
/// HTTP-specific pagination provider with support for custom request handling, retry policies, and self-healing.
/// 
/// This class handles:
/// - Building and executing HTTP requests
/// - Parsing HTTP responses
/// - Automatic retry with configurable policies
/// - Self-healing on failures
/// - Resource management (HttpClient ownership tracking)
/// 
/// For detailed retry logic, see HttpPaginationProviderRetryLogic.cs
/// </summary>
public abstract partial class HttpPaginationProvider<TData>(ILogger logger) : PaginationDataProviderBase<HttpPaginationRequest, TData>(logger), IDisposable
{
    private bool _disposed;
    private HttpClient _httpClient;
    private bool _ownsHttpClient;
    private RetryPolicy? _retryPolicy;
    
    protected HttpClient CurrentHttpClient
    {
        get
        {
            if (_httpClient == null)
            {
                _httpClient = CreateHttpClient() ?? throw new ArgumentNullException(nameof(_httpClient), "HttpClient cannot be null");
                _ownsHttpClient = true;
            }
            return _httpClient;
        }
    }

    protected abstract HttpClient CreateHttpClient();

    /// <summary>
    /// Enable retry functionality with the specified retry policy.
    /// This provides a fluent API for configuring retry behavior.
    /// </summary>
    /// <param name="retryPolicy">The retry policy to use. If null, uses default exponential backoff.</param>
    /// <returns>The current instance for method chaining</returns>
    public HttpPaginationProvider<TData> AllowRetry(RetryPolicy? retryPolicy = null)
    {
        _retryPolicy = retryPolicy ?? RetryPolicy.ExponentialBackoff();
        return this;
    }

    /// <summary>
    /// Disable retry functionality.
    /// </summary>
    /// <returns>The current instance for method chaining</returns>
    public HttpPaginationProvider<TData> DisableRetry()
    {
        _retryPolicy = null;
        return this;
    }

    /// <summary>
    /// Allow users to dynamically replace the HttpClient instance.
    /// This is useful for scenarios like proxy switching or connection pooling changes.
    /// </summary>
    /// <param name="httpClient">New HttpClient instance to use. We don't own externally provided clients.</param>
    public void SetHttpClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        ValidateHttpClientSecurity(httpClient);

        // Only dispose if we own the client
        if (_httpClient != null && _ownsHttpClient)
        {
            _httpClient.Dispose();
        }

        _httpClient = httpClient;
        _ownsHttpClient = false; // We don't own externally provided clients
        _logger.LogInformation("HttpClient has been replaced with a new instance");
    }

    private static void ValidateHttpClientSecurity(HttpClient client)
    {
        // Ensure finite timeout
        if (client.Timeout == Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentException(
                "HttpClient must have a finite timeout to prevent hanging requests. " +
                "Set Timeout property to a reasonable value (e.g., 30 seconds).",
                nameof(client));
        }
    }

    /// <summary>
    /// Called when an HTTP request fails. Override to handle failures (e.g., switch proxies)
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="request">The pagination request that failed</param>
    /// <param name="attemptNumber">The attempt number (1-based)</param>
    protected virtual void OnHttpRequestFailed(Exception exception, PaginationRequest<HttpPaginationRequest> request, int attemptNumber = 1)
    {
        // Default implementation: log and continue
        _logger.LogWarning(exception, "HTTP request failed for {Identifier} at offset {Offset} (attempt {Attempt})",
            request.Identifier, request.Offset, attemptNumber);
    }

    /// <summary>
    /// Called before a retry attempt. Override to customize retry behavior.
    /// </summary>
    /// <param name="exception">The exception that triggered the retry</param>
    /// <param name="request">The pagination request being retried</param>
    /// <param name="attemptNumber">The upcoming attempt number</param>
    /// <param name="delayMs">The delay in milliseconds before the retry</param>
    protected virtual void OnBeforeRetry(Exception exception, PaginationRequest<HttpPaginationRequest> request, int attemptNumber, int delayMs)
    {
        _logger.LogInformation("Retrying request for {Identifier} (attempt {Attempt}) after {Delay}ms delay",
            request.Identifier, attemptNumber, delayMs);
    }

    /// <summary>
    /// Called when all retry attempts have been exhausted.
    /// </summary>
    /// <param name="exception">The last exception that occurred</param>
    /// <param name="request">The pagination request that failed</param>
    /// <param name="totalAttempts">Total number of attempts made</param>
    protected virtual void OnRetryExhausted(Exception exception, PaginationRequest<HttpPaginationRequest> request, int totalAttempts)
    {
        _logger.LogError(exception, "All {TotalAttempts} retry attempts exhausted for {Identifier}",
            totalAttempts, request.Identifier);
    }

    /// <summary>
    /// Override this method to provide custom self-healing logic.
    /// </summary>
    protected virtual Task<SelfHealingResult> AttemptSelfHealingAsync(SelfHealingContext context)
    {
        return Task.FromResult(SelfHealingResult.Success());
    }

    /// <summary>
    /// Called when an HTTP response has a non-success status code. Override to handle response errors.
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="request">The pagination request</param>
    /// <param name="parsedResult">The parsed result from ParseResponse</param>
    protected virtual void OnHttpResponseError(HttpResponseMessage response, PaginationRequest<HttpPaginationRequest> request, PaginationResponse<TData> parsedResult)
    {
        // Default implementation: log warning
        _logger.LogWarning("HTTP {StatusCode} received for {Identifier} at offset {Offset}: {ReasonPhrase}",
            response.StatusCode, request.Identifier, request.Offset, response.ReasonPhrase);
    }

    /// <summary>
    /// Called when an HTTP response is successful. Override to handle successful responses.
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="request">The pagination request</param>
    /// <param name="parsedResult">The parsed result from ParseResponse</param>
    protected virtual void OnHttpResponseSuccess(HttpResponseMessage response, PaginationRequest<HttpPaginationRequest> request, PaginationResponse<TData> parsedResult)
    {
        // Default implementation: log trace
        _logger.LogTrace("HTTP {StatusCode} received successfully for {Identifier} at offset {Offset}",
            response.StatusCode, request.Identifier, request.Offset);
    }

    /// <summary>
    /// Build request URL for specific offset/limit
    /// </summary>
    /// <param name="baseRequest">Base HTTP request</param>
    /// <param name="offset">Current offset</param>
    /// <param name="limit">Items per page</param>
    /// <returns>Complete request URL</returns>
    protected abstract string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit);

    /// <summary>
    /// Parse HTTP response content
    /// </summary>
    /// <param name="content">Response content</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <returns>Parsed pagination response</returns>
    protected abstract PaginationResponse<TData> ParseResponse(string content, HttpStatusCode statusCode);

    /// <summary>
    /// Create HTTP request message
    /// </summary>
    /// <param name="request">Pagination request</param>
    /// <returns>HTTP request message</returns>
    protected virtual HttpRequestMessage CreateHttpRequest(PaginationRequest<HttpPaginationRequest> request)
    {
        var url = BuildRequestUrl(request.Request, request.Offset, request.Limit);
        var httpRequest = new HttpRequestMessage(request.Request.Method, url);

        // Add custom headers
        if (request.Request.Headers != null)
        {
            foreach (var header in request.Request.Headers)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Add body for POST/PUT requests
        if (!string.IsNullOrEmpty(request.Request.Body) &&
            (request.Request.Method == HttpMethod.Post || request.Request.Method == HttpMethod.Put))
        {
            httpRequest.Content = new StringContent(request.Request.Body,
                System.Text.Encoding.UTF8, "application/json");
        }

        return httpRequest;
    }

    /// <summary>
    /// Send the HTTP request with customizable behavior.
    /// Override this method to add custom logic such as:
    /// - Adding custom headers (authentication, tracing)
    /// - Implementing logging or monitoring
    /// - Applying custom retry logic or circuit breaker patterns
    /// - Request/response interception or transformation
    /// </summary>
    protected virtual async Task<HttpResponseMessage> SendHttpRequestAsync(
        HttpClient httpClient,
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        return await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetch a single page via HTTP
    /// </summary>
    protected sealed override async Task<PaginationResponse<TData>> FetchPageAsync(
        PaginationRequest<HttpPaginationRequest> request,
        CancellationToken cancellationToken)
    {
        // If retry is not enabled, execute once with basic error handling
        if (_retryPolicy == null || _retryPolicy.Strategy == RetryStrategy.None)
        {
            try
            {
                return await FetchPageWithoutRetryAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnHttpRequestFailed(ex, request, 1);
                
                // Return error response instead of throwing
                return new PaginationResponse<TData>
                {
                    Data = null,
                    HasMore = false,
                    ErrorMessage = ex is TaskCanceledException tce && tce.InnerException is TimeoutException
                        ? "Request timeout"
                        : ex is HttpRequestException
                            ? $"HTTP error: {ex.Message}"
                            : ex.Message
                };
            }
        }

        return await ExecuteWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetch a single page without retry logic
    /// </summary>
    private async Task<PaginationResponse<TData>> FetchPageWithoutRetryAsync(
        PaginationRequest<HttpPaginationRequest> request,
        CancellationToken cancellationToken,
        int attemptNumber = 1,
        object? healingState = null)
    {
        // Capture the HttpClient instance at the start of the request
        // This ensures consistency even if SetHttpClient is called during execution
        var httpClient = CurrentHttpClient;

        try
        {
            using var httpRequest = CreateHttpRequest(request);

            _logger.LogTrace("Making HTTP {Method} request to: {Url} (attempt {Attempt})",
                httpRequest.Method, httpRequest.RequestUri, attemptNumber);

            // Use the virtual SendHttpRequestAsync method to allow customization
            using var response = await SendHttpRequestAsync(httpClient, httpRequest, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                return new PaginationResponse<TData>
                {
                    Data = new List<TData>(),
                    HasMore = false,
                    ErrorMessage = "Empty response received"
                };
            }

            var result = ParseResponse(content, response.StatusCode);

            // Check for HTTP errors
            if (!response.IsSuccessStatusCode)
            {
                OnHttpResponseError(response, request, result); // Notify user of response error

                result.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                // Ensure error state
                result.Data ??= new List<TData>();
                result.HasMore = false;
            }
            else
            {
                OnHttpResponseSuccess(response, request, result); // Notify user of successful response
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred for request {Identifier} at offset {Offset} (attempt {Attempt})",
                request.Identifier, request.Offset, attemptNumber);

            throw; // Re-throw to be handled by retry logic
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timeout for {Identifier} at offset {Offset} (attempt {Attempt})",
                request.Identifier, request.Offset, attemptNumber);

            throw; // Re-throw to be handled by retry logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for request {Identifier} at offset {Offset} (attempt {Attempt})",
                request.Identifier, request.Offset, attemptNumber);

            throw; // Re-throw to be handled by retry logic
        }
    }

    /// <summary>
    /// Build URL with query parameters
    /// </summary>
    /// <param name="baseUrl">Base URL</param>
    /// <param name="parameters">Query parameters</param>
    /// <returns>Complete URL</returns>
    protected static string BuildUrl(string baseUrl, Dictionary<string, string>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return baseUrl;

        var queryString = string.Join("&", parameters.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{baseUrl}?{queryString}";
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose managed resources.
    /// Only disposes HttpClient if we own it (created internally).
    /// Externally provided HttpClient instances are left to the caller for disposal.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing && _httpClient != null && _ownsHttpClient)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }
}