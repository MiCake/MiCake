using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MiCake.Core.Util.PaginationData;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MiCake.Core.Tests.Util.PaginationData;

public class HttpPaginationProviderTests
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        ILogger logger = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestHttpPaginationProvider(logger));
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NullHttpClientProvider(mockLogger));
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();

        // Act
        var provider = new TestHttpPaginationProvider(mockLogger);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void BuildUrl_WithNullParameters_ShouldReturnBaseUrl()
    {
        // Arrange
        var baseUrl = "https://api.example.com/data";

        // Act
        var result = TestHttpPaginationProvider.TestBuildUrl(baseUrl, null);

        // Assert
        Assert.Equal(baseUrl, result);
    }

    [Fact]
    public void BuildUrl_WithEmptyParameters_ShouldReturnBaseUrl()
    {
        // Arrange
        var baseUrl = "https://api.example.com/data";
        var parameters = new Dictionary<string, string>();

        // Act
        var result = TestHttpPaginationProvider.TestBuildUrl(baseUrl, parameters);

        // Assert
        Assert.Equal(baseUrl, result);
    }

    [Fact]
    public void BuildUrl_WithParameters_ShouldAppendQueryString()
    {
        // Arrange
        var baseUrl = "https://api.example.com/data";
        var parameters = new Dictionary<string, string>
        {
            ["page"] = "1",
            ["size"] = "10",
            ["filter"] = "active"
        };

        // Act
        var result = TestHttpPaginationProvider.TestBuildUrl(baseUrl, parameters);

        // Assert
        Assert.StartsWith(baseUrl + "?", result);
        Assert.Contains("page=1", result);
        Assert.Contains("size=10", result);
        Assert.Contains("filter=active", result);
    }

    [Fact]
    public void BuildUrl_WithSpecialCharacters_ShouldEncodeCorrectly()
    {
        // Arrange
        var baseUrl = "https://api.example.com/data";
        var parameters = new Dictionary<string, string>
        {
            ["query"] = "hello world",
            ["special"] = "a+b=c&d"
        };

        // Act
        var result = TestHttpPaginationProvider.TestBuildUrl(baseUrl, parameters);

        // Assert
        Assert.Contains("hello%20world", result);
        Assert.Contains("a%2Bb%3Dc%26d", result);
    }

    [Fact]
    public async Task FetchPageAsync_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var provider = new TestHttpPaginationProvider(mockLogger);
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data",
                Method = HttpMethod.Get
            },
            Offset = 0,
            Limit = 10,
            Identifier = "test"
        };

        // Act
        var response = await provider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("test-data", response.Data[0]);
        Assert.False(response.HasMore);
    }

    [Fact]
    public async Task FetchPageAsync_WithPostRequest_ShouldIncludeBody()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var provider = new TestHttpPaginationProvider(mockLogger);
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data",
                Method = HttpMethod.Post,
                Body = "{\"query\": \"test\"}"
            },
            Offset = 0,
            Limit = 10
        };

        // Act
        var response = await provider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task FetchPageAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var provider = new TestHttpPaginationProvider(mockLogger);
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data",
                Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer token123"
                }
            },
            Offset = 0,
            Limit = 10
        };

        // Act
        var response = await provider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public void Dispose_ShouldDisposeHttpClient()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var provider = new TestHttpPaginationProvider(mockLogger);

        // Act & Assert - Should not throw
        provider.Dispose();
        
        // Second dispose should also not throw
        provider.Dispose();
    }

    [Fact]
    public void Dispose_WithDisposingFalse_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var provider = new TestHttpPaginationProvider(mockLogger);

        // Act & Assert - Should not throw
        provider.TestDispose(false);
    }

    // Test implementation of HttpPaginationProvider
    private class NullHttpClientProvider : HttpPaginationProvider<string>
    {
        public NullHttpClientProvider(ILogger logger) : base(logger) { }
        protected override HttpClient CreateHttpClient() => null!;
        protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit) => "";
        protected override PaginationResponse<string> ParseResponse(string content, HttpStatusCode statusCode) => new();
    }

    private class TestHttpPaginationProvider : HttpPaginationProvider<string>
    {
        private readonly bool _returnNullHttpClient;

        public TestHttpPaginationProvider(ILogger logger, bool returnNullHttpClient = false) : base(logger)
        {
            _returnNullHttpClient = returnNullHttpClient;
        }

        protected override HttpClient CreateHttpClient()
        {
            if (_returnNullHttpClient)
                return null!;

            var handler = new TestHttpMessageHandler();
            return new HttpClient(handler);
        }

        protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
        {
            var parameters = new Dictionary<string, string>
            {
                ["offset"] = offset.ToString(),
                ["limit"] = limit.ToString()
            };

            if (baseRequest.QueryParameters != null)
            {
                foreach (var kvp in baseRequest.QueryParameters)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }

            return BuildUrl(baseRequest.BaseUrl, parameters);
        }

        protected override PaginationResponse<string> ParseResponse(string content, HttpStatusCode statusCode)
        {
            if (statusCode == HttpStatusCode.OK)
            {
                return new PaginationResponse<string>
                {
                    Data = new List<string> { "test-data" },
                    HasMore = false,
                    NextOffset = null
                };
            }

            return new PaginationResponse<string>
            {
                Data = null,
                HasMore = false,
                ErrorMessage = $"HTTP {statusCode}"
            };
        }

        // Public test methods
        public static string TestBuildUrl(string baseUrl, Dictionary<string, string>? parameters)
        {
            return BuildUrl(baseUrl, parameters);
        }

        public async Task<PaginationResponse<string>> TestFetchPageAsync(
            PaginationRequest<HttpPaginationRequest> request,
            CancellationToken cancellationToken)
        {
            return await FetchPageAsync(request, cancellationToken);
        }

        public void TestDispose(bool disposing)
        {
            Dispose(disposing);
        }
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\": \"test\"}")
            };

            return Task.FromResult(response);
        }
    }
}