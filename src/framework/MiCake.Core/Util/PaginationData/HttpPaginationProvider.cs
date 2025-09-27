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
        try
        {
            using var httpRequest = CreateHttpRequest(request);

            _logger.LogTrace("Making HTTP {Method} request to: {Url}",
                httpRequest.Method, httpRequest.RequestUri);

            using var response = await CurrentHttpClient.SendAsync(httpRequest, cancellationToken);
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

            // Log HTTP errors
            if (!response.IsSuccessStatusCode && result.IsSuccess)
            {
                _logger.LogWarning("HTTP {StatusCode} received: {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);

                result.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
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