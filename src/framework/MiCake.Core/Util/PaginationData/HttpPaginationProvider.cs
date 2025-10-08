using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiCake.Core.Util.PaginationData;

/// <summary>
/// HTTP-specific pagination provider
/// </summary>
public abstract class HttpPaginationProvider<TData>(ILogger logger) : PaginationDataProviderBase<HttpPaginationRequest, TData>(logger), IDisposable
{
    private bool _disposed;
    private HttpClient _httpClient;
    protected HttpClient CurrentHttpClient
    {
        get
        {
            _httpClient ??= CreateHttpClient() ?? throw new ArgumentNullException(nameof(_httpClient), "HttpClient cannot be null");
            return _httpClient;
        }
    }

    protected abstract HttpClient CreateHttpClient();

    /// <summary>
    /// Allow users to dynamically replace the HttpClient instance
    /// This is useful for scenarios like proxy switching or connection pooling changes
    /// </summary>
    /// <param name="httpClient">New HttpClient instance to use</param>
    public void SetHttpClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        // Dispose existing client if it was created internally
        if (_httpClient != null && _httpClient != httpClient)
        {
            _httpClient.Dispose();
        }

        _httpClient = httpClient;
        _logger.LogInformation("HttpClient has been replaced with a new instance");
    }

    /// <summary>
    /// Called when an HTTP request fails. Override to handle failures (e.g., switch proxies)
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="request">The pagination request that failed</param>
    protected virtual void OnHttpRequestFailed(Exception exception, PaginationRequest<HttpPaginationRequest> request)
    {
        // Default implementation: log and continue
        _logger.LogWarning(exception, "HTTP request failed for {Identifier} at offset {Offset}",
            request.Identifier, request.Offset);
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
    /// Fetch a single page via HTTP
    /// </summary>
    protected sealed override async Task<PaginationResponse<TData>> FetchPageAsync(
        PaginationRequest<HttpPaginationRequest> request,
        CancellationToken cancellationToken)
    {
        // Capture the HttpClient instance at the start of the request
        // This ensures consistency even if SetHttpClient is called during execution
        var httpClient = CurrentHttpClient;

        try
        {
            using var httpRequest = CreateHttpRequest(request);

            _logger.LogTrace("Making HTTP {Method} request to: {Url}",
                httpRequest.Method, httpRequest.RequestUri);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

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
            OnHttpRequestFailed(ex, request); // Notify user of failure

            _logger.LogError(ex, "HTTP error occurred for request {Identifier} at offset {Offset}",
                request.Identifier, request.Offset);

            return new PaginationResponse<TData>
            {
                Data = null,
                HasMore = false,
                ErrorMessage = $"HTTP error: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            OnHttpRequestFailed(ex, request); // Notify user of timeout failure

            _logger.LogError(ex, "Request timeout for {Identifier} at offset {Offset}",
                request.Identifier, request.Offset);

            return new PaginationResponse<TData>
            {
                Data = null,
                HasMore = false,
                ErrorMessage = "Request timeout"
            };
        }
        catch (Exception ex)
        {
            OnHttpRequestFailed(ex, request); // Notify user of unexpected failure

            _logger.LogError(ex, "Unexpected error for request {Identifier} at offset {Offset}",
                request.Identifier, request.Offset);

            return new PaginationResponse<TData>
            {
                Data = null,
                HasMore = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
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
    /// Dispose managed resources
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}