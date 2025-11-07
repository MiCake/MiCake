using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MiCake.Core.Util.PaginationData;
using Moq;
using Xunit;

namespace MiCake.Core.Tests.Util.PaginationData;

public class HttpPaginationProviderRetryTests
{
    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestHttpPaginationProvider : HttpPaginationProvider<TestData>
    {
        private readonly HttpClient _httpClient;
        private int _requestCount = 0;
        private readonly int _failUntilAttempt;
        private readonly List<string> _healingLog = new();

        public int RequestCount => _requestCount;
        public IReadOnlyList<string> HealingLog => _healingLog;

        public TestHttpPaginationProvider(
            ILogger<TestHttpPaginationProvider> logger,
            HttpClient httpClient,
            int failUntilAttempt = 0) : base(logger)
        {
            _httpClient = httpClient;
            _failUntilAttempt = failUntilAttempt;
        }

        protected override HttpClient CreateHttpClient() => _httpClient;

        protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
        {
            return $"{baseRequest.BaseUrl}?offset={offset}&limit={limit}";
        }

        protected override PaginationResponse<TestData> ParseResponse(string content, HttpStatusCode statusCode)
        {
            if (statusCode != HttpStatusCode.OK)
            {
                return new PaginationResponse<TestData>
                {
                    Data = new List<TestData>(),
                    HasMore = false,
                    ErrorMessage = $"HTTP {statusCode}"
                };
            }

            // Simple JSON parsing for test
            var data = new List<TestData> { new() { Id = 1, Name = "Test" } };
            return new PaginationResponse<TestData>
            {
                Data = data,
                HasMore = false
            };
        }

        protected override async Task<SelfHealingResult> AttemptSelfHealingAsync(SelfHealingContext context)
        {
            _healingLog.Add($"Attempt {context.AttemptNumber}: {context.Exception.Message}");
            return await Task.FromResult(SelfHealingResult.Success("Self-healing executed"));
        }

        protected override void OnHttpRequestFailed(Exception exception, PaginationRequest<HttpPaginationRequest> request, int attemptNumber = 1)
        {
            _requestCount = attemptNumber;
            base.OnHttpRequestFailed(exception, request, attemptNumber);
        }

        // Expose protected method for testing
        public async Task<PaginationResponse<TestData>> TestFetchPageAsync(
            PaginationRequest<HttpPaginationRequest> request,
            CancellationToken cancellationToken)
        {
            return await base.FetchPageAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task FetchPageAsync_WithRetryPolicy_ShouldRetryOnFailure()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TestHttpPaginationProvider>>();
        var mockHandler = new MockHttpMessageHandler();
        
        // First request fails, second succeeds
        mockHandler.AddResponse(HttpStatusCode.InternalServerError, "Error");
        mockHandler.AddResponse(HttpStatusCode.OK, "{\"data\":[]}");
        
        var httpClient = new HttpClient(mockHandler);
        var provider = new TestHttpPaginationProvider(mockLogger.Object, httpClient);
        provider.AllowRetry(RetryPolicy.FixedDelay(maxAttempts: 3, delayMs: 10));

        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest { BaseUrl = "https://api.test.com/data" },
            Offset = 0,
            Limit = 10
        };

        // Act
        var result = await provider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Data != null || result.ErrorMessage != null);
        Assert.Equal(2, mockHandler.RequestCount); // Should have made 2 requests
    }

    [Fact]
    public async Task FetchPageAsync_NoRetryPolicy_ShouldNotRetry()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TestHttpPaginationProvider>>();
        var mockHandler = new MockHttpMessageHandler();
        
        mockHandler.AddResponse(HttpStatusCode.InternalServerError, "Error");
        
        var httpClient = new HttpClient(mockHandler);
        var provider = new TestHttpPaginationProvider(mockLogger.Object, httpClient);
        
        // No retry policy set (default is null)

        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest { BaseUrl = "https://api.test.com/data" },
            Offset = 0,
            Limit = 10
        };

        // Act
        var result = await provider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(1, mockHandler.RequestCount); // Should only make 1 request
    }

    [Fact]
    public async Task FetchPageAsync_ExhaustedRetries_ShouldReturnError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TestHttpPaginationProvider>>();
        var mockHandler = new MockHttpMessageHandler();
        
        // All requests fail
        for (int i = 0; i < 5; i++)
        {
            mockHandler.AddResponse(HttpStatusCode.InternalServerError, "Error");
        }
        
        var httpClient = new HttpClient(mockHandler);
        var provider = new TestHttpPaginationProvider(mockLogger.Object, httpClient);
        provider.AllowRetry(RetryPolicy.FixedDelay(maxAttempts: 3, delayMs: 10));

        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest { BaseUrl = "https://api.test.com/data" },
            Offset = 0,
            Limit = 10,
            Identifier = "test"
        };

        // Act
        var result = await provider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("failed after", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchPageAsync_SelfHealing_ShouldBeInvoked()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TestHttpPaginationProvider>>();
        var mockHandler = new MockHttpMessageHandler();
        
        // Fail twice, then succeed
        mockHandler.AddResponse(HttpStatusCode.InternalServerError, "Error 1");
        mockHandler.AddResponse(HttpStatusCode.InternalServerError, "Error 2");
        mockHandler.AddResponse(HttpStatusCode.OK, "{\"data\":[]}");
        
        var httpClient = new HttpClient(mockHandler);
        var provider = new TestHttpPaginationProvider(mockLogger.Object, httpClient);
        provider.AllowRetry(RetryPolicy.FixedDelay(maxAttempts: 3, delayMs: 10));

        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest { BaseUrl = "https://api.test.com/data" },
            Offset = 0,
            Limit = 10
        };

        // Act
        var result = await provider.TestFetchPageAsync(request, CancellationToken.None);

        // Assert
        Assert.True(provider.HealingLog.Count >= 1, "Self-healing should have been invoked");
    }

    [Fact]
    public async Task FetchPageAsync_ExponentialBackoff_ShouldIncreaseDelay()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TestHttpPaginationProvider>>();
        var mockHandler = new MockHttpMessageHandler();
        
        // Fail 3 times, then succeed
        for (int i = 0; i < 3; i++)
        {
            mockHandler.AddResponse(HttpStatusCode.ServiceUnavailable, "Service Unavailable");
        }
        mockHandler.AddResponse(HttpStatusCode.OK, "{\"data\":[]}");
        
        var httpClient = new HttpClient(mockHandler);
        var provider = new TestHttpPaginationProvider(mockLogger.Object, httpClient);
        provider.AllowRetry(RetryPolicy.ExponentialBackoff(maxAttempts: 5, initialDelayMs: 50, multiplier: 2.0));

        var request = new PaginationRequest<HttpPaginationRequest>
        {
            Request = new HttpPaginationRequest { BaseUrl = "https://api.test.com/data" },
            Offset = 0,
            Limit = 10
        };

        var startTime = DateTime.UtcNow;

        // Act
        var result = await provider.TestFetchPageAsync(request, CancellationToken.None);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Assert
        // With exponential backoff: 50ms + 100ms + 200ms = 350ms minimum
        // We add some tolerance for execution time
        Assert.True(elapsed >= 250, $"Expected at least 250ms delay, got {elapsed}ms");
    }

    // Helper class to mock HttpMessageHandler
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode StatusCode, string Content)> _responses = new();
        private int _requestCount = 0;

        public int RequestCount => _requestCount;

        public void AddResponse(HttpStatusCode statusCode, string content)
        {
            _responses.Enqueue((statusCode, content));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requestCount++;
            
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":[]}")
                });
            }

            var (statusCode, content) = _responses.Dequeue();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
        }
    }
}
