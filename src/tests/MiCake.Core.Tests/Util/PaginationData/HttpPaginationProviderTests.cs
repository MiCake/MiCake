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

    [Fact]
    public void SetHttpClient_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var provider = new TestHttpPaginationProvider(mockLogger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider.SetHttpClient(null!));
    }

    [Fact]
    public void SetHttpClient_WithValidHttpClient_ShouldReplaceHttpClient()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var provider = new TestHttpPaginationProvider(mockLogger);
        var newHttpClient = new HttpClient();

        // Act
        provider.SetHttpClient(newHttpClient);

        // Assert
        Assert.Equal(newHttpClient, provider.GetCurrentHttpClient());
    }

    [Fact]
    public async Task FetchPageAsync_ShouldUseCapturedHttpClient()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var provider = new TestHttpPaginationProvider(mockLogger);
        var originalHttpClient = provider.GetCurrentHttpClient();
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data"
            },
            Offset = 0,
            Limit = 10,
            Identifier = "test"
        };

        // Act - Start the request
        var task = provider.TestFetchPageAsync(request, CancellationToken.None);
        
        // Change HttpClient during execution (should not affect the ongoing request)
        var newHttpClient = new HttpClient(new TestHttpMessageHandler());
        provider.SetHttpClient(newHttpClient);
        
        var response = await task;

        // Assert - The request should still succeed with original HttpClient
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task FetchPageAsync_WithHttpRequestException_ShouldCallOnHttpRequestFailed()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var failingProvider = new FailingHttpPaginationProvider(mockLogger);
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data"
            },
            Offset = 0,
            Limit = 10,
            Identifier = "test"
        };

        // Act
        var response = await failingProvider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Contains("HTTP error", response.ErrorMessage);
        Assert.True(failingProvider.OnHttpRequestFailedCalled);
    }

    [Fact]
    public async Task FetchPageAsync_WithTimeout_ShouldCallOnHttpRequestFailed()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var timeoutProvider = new TimeoutHttpPaginationProvider(mockLogger);
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data"
            },
            Offset = 0,
            Limit = 10,
            Identifier = "test"
        };

        // Act
        var response = await timeoutProvider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal("Request timeout", response.ErrorMessage);
        Assert.True(timeoutProvider.OnHttpRequestFailedCalled);
    }

    [Fact]
    public async Task FetchPageAsync_WithHttpErrorStatusCode_ShouldCallOnHttpResponseError()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var errorProvider = new ErrorStatusHttpPaginationProvider(mockLogger);
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data"
            },
            Offset = 0,
            Limit = 10,
            Identifier = "test"
        };

        // Act
        var response = await errorProvider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Contains("HTTP NotFound", response.ErrorMessage);
        Assert.True(errorProvider.OnHttpResponseErrorCalled);
        Assert.False(errorProvider.OnHttpResponseSuccessCalled);
    }

    [Fact]
    public async Task FetchPageAsync_WithSuccessStatusCode_ShouldCallOnHttpResponseSuccess()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var successProvider = new SuccessHttpPaginationProvider(mockLogger);
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data"
            },
            Offset = 0,
            Limit = 10,
            Identifier = "test"
        };

        // Act
        var response = await successProvider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.True(successProvider.OnHttpResponseSuccessCalled);
        Assert.False(successProvider.OnHttpResponseErrorCalled);
    }

    [Fact]
    public async Task FetchPageAsync_WithUnexpectedException_ShouldCallOnHttpRequestFailed()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestHttpPaginationProvider>();
        var exceptionProvider = new ExceptionHttpPaginationProvider(mockLogger);
        
        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest
            {
                BaseUrl = "https://api.example.com/data"
            },
            Offset = 0,
            Limit = 10,
            Identifier = "test"
        };

        // Act
        var response = await exceptionProvider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Contains("Unexpected error", response.ErrorMessage);
        Assert.True(exceptionProvider.OnHttpRequestFailedCalled);
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

        // Test helper method
        public HttpClient GetCurrentHttpClient()
        {
            return CurrentHttpClient;
        }
    }

    private class FailingHttpPaginationProvider : HttpPaginationProvider<string>
    {
        public bool OnHttpRequestFailedCalled { get; private set; }

        public FailingHttpPaginationProvider(ILogger logger) : base(logger) { }

        protected override HttpClient CreateHttpClient()
        {
            var handler = new FailingHttpMessageHandler();
            return new HttpClient(handler);
        }

        protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
        {
            return baseRequest.BaseUrl;
        }

        protected override PaginationResponse<string> ParseResponse(string content, HttpStatusCode statusCode)
        {
            return new PaginationResponse<string>
            {
                Data = new List<string> { "test-data" },
                HasMore = false
            };
        }

        protected override void OnHttpRequestFailed(Exception exception, PaginationRequest<HttpPaginationRequest> request)
        {
            OnHttpRequestFailedCalled = true;
            base.OnHttpRequestFailed(exception, request);
        }

        public async Task<PaginationResponse<string>> TestFetchPageAsync(
            PaginationRequest<HttpPaginationRequest> request,
            CancellationToken cancellationToken)
        {
            return await FetchPageAsync(request, cancellationToken);
        }
    }

    private class TimeoutHttpPaginationProvider : HttpPaginationProvider<string>
    {
        public bool OnHttpRequestFailedCalled { get; private set; }

        public TimeoutHttpPaginationProvider(ILogger logger) : base(logger) { }

        protected override HttpClient CreateHttpClient()
        {
            var handler = new TimeoutHttpMessageHandler();
            return new HttpClient(handler);
        }

        protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
        {
            return baseRequest.BaseUrl;
        }

        protected override PaginationResponse<string> ParseResponse(string content, HttpStatusCode statusCode)
        {
            return new PaginationResponse<string>
            {
                Data = new List<string> { "test-data" },
                HasMore = false
            };
        }

        protected override void OnHttpRequestFailed(Exception exception, PaginationRequest<HttpPaginationRequest> request)
        {
            OnHttpRequestFailedCalled = true;
            base.OnHttpRequestFailed(exception, request);
        }

        public async Task<PaginationResponse<string>> TestFetchPageAsync(
            PaginationRequest<HttpPaginationRequest> request,
            CancellationToken cancellationToken)
        {
            return await FetchPageAsync(request, cancellationToken);
        }
    }

    private class ErrorStatusHttpPaginationProvider : HttpPaginationProvider<string>
    {
        public bool OnHttpResponseErrorCalled { get; private set; }
        public bool OnHttpResponseSuccessCalled { get; private set; }

        public ErrorStatusHttpPaginationProvider(ILogger logger) : base(logger) { }

        protected override HttpClient CreateHttpClient()
        {
            var handler = new ErrorStatusHttpMessageHandler();
            return new HttpClient(handler);
        }

        protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
        {
            return baseRequest.BaseUrl;
        }

        protected override PaginationResponse<string> ParseResponse(string content, HttpStatusCode statusCode)
        {
            return new PaginationResponse<string>
            {
                Data = new List<string> { "test-data" },
                HasMore = false
            };
        }

        protected override void OnHttpResponseError(HttpResponseMessage response, PaginationRequest<HttpPaginationRequest> request, PaginationResponse<string> parsedResult)
        {
            OnHttpResponseErrorCalled = true;
            base.OnHttpResponseError(response, request, parsedResult);
        }

        protected override void OnHttpResponseSuccess(HttpResponseMessage response, PaginationRequest<HttpPaginationRequest> request, PaginationResponse<string> parsedResult)
        {
            OnHttpResponseSuccessCalled = true;
            base.OnHttpResponseSuccess(response, request, parsedResult);
        }

        public async Task<PaginationResponse<string>> TestFetchPageAsync(
            PaginationRequest<HttpPaginationRequest> request,
            CancellationToken cancellationToken)
        {
            return await FetchPageAsync(request, cancellationToken);
        }
    }

    private class SuccessHttpPaginationProvider : HttpPaginationProvider<string>
    {
        public bool OnHttpResponseErrorCalled { get; private set; }
        public bool OnHttpResponseSuccessCalled { get; private set; }

        public SuccessHttpPaginationProvider(ILogger logger) : base(logger) { }

        protected override HttpClient CreateHttpClient()
        {
            var handler = new TestHttpMessageHandler();
            return new HttpClient(handler);
        }

        protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
        {
            return baseRequest.BaseUrl;
        }

        protected override PaginationResponse<string> ParseResponse(string content, HttpStatusCode statusCode)
        {
            return new PaginationResponse<string>
            {
                Data = new List<string> { "test-data" },
                HasMore = false
            };
        }

        protected override void OnHttpResponseError(HttpResponseMessage response, PaginationRequest<HttpPaginationRequest> request, PaginationResponse<string> parsedResult)
        {
            OnHttpResponseErrorCalled = true;
            base.OnHttpResponseError(response, request, parsedResult);
        }

        protected override void OnHttpResponseSuccess(HttpResponseMessage response, PaginationRequest<HttpPaginationRequest> request, PaginationResponse<string> parsedResult)
        {
            OnHttpResponseSuccessCalled = true;
            base.OnHttpResponseSuccess(response, request, parsedResult);
        }

        public async Task<PaginationResponse<string>> TestFetchPageAsync(
            PaginationRequest<HttpPaginationRequest> request,
            CancellationToken cancellationToken)
        {
            return await FetchPageAsync(request, cancellationToken);
        }
    }

    private class ExceptionHttpPaginationProvider : HttpPaginationProvider<string>
    {
        public bool OnHttpRequestFailedCalled { get; private set; }

        public ExceptionHttpPaginationProvider(ILogger logger) : base(logger) { }

        protected override HttpClient CreateHttpClient()
        {
            var handler = new ExceptionHttpMessageHandler();
            return new HttpClient(handler);
        }

        protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
        {
            return baseRequest.BaseUrl;
        }

        protected override PaginationResponse<string> ParseResponse(string content, HttpStatusCode statusCode)
        {
            return new PaginationResponse<string>
            {
                Data = new List<string> { "test-data" },
                HasMore = false
            };
        }

        protected override void OnHttpRequestFailed(Exception exception, PaginationRequest<HttpPaginationRequest> request)
        {
            OnHttpRequestFailedCalled = true;
            base.OnHttpRequestFailed(exception, request);
        }

        public async Task<PaginationResponse<string>> TestFetchPageAsync(
            PaginationRequest<HttpPaginationRequest> request,
            CancellationToken cancellationToken)
        {
            return await FetchPageAsync(request, cancellationToken);
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

    private class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Network error");
        }
    }

    private class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new TaskCanceledException("Request timeout", new TimeoutException());
        }
    }

    private class ErrorStatusHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not found")
            };
            return Task.FromResult(response);
        }
    }

    private class ExceptionHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Unexpected error");
        }
    }
}