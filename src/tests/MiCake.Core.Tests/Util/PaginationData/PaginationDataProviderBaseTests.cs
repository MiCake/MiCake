using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiCake.Core.Util.PaginationData;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MiCake.Core.Tests.Util.PaginationData;

public class PaginationDataProviderBaseTests
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        ILogger logger = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestPaginationDataProvider(logger));
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldInitializeCorrectly()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();

        // Act
        var provider = new TestPaginationDataProvider(mockLogger);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task LoadPaginatedDataAsync_WithSinglePage_ShouldReturnAllData()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();
        var provider = new TestPaginationDataProvider(mockLogger);
        provider.SetupPages(new[]
        {
            new List<string> { "item1", "item2", "item3" }
        });

        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 10,
            MaxPages = 5,
            DelayBetweenRequests = 0
        };

        // Act
        var result = await provider.TestLoadPaginatedDataAsync("test-request", config, "test-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("item1", result);
        Assert.Contains("item2", result);
        Assert.Contains("item3", result);
    }

    [Fact]
    public async Task LoadPaginatedDataAsync_WithMultiplePages_ShouldReturnAllData()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();
        var provider = new TestPaginationDataProvider(mockLogger);
        provider.SetupPages(new[]
        {
            new List<string> { "page1-item1", "page1-item2" },
            new List<string> { "page2-item1", "page2-item2" },
            new List<string> { "page3-item1" }
        });

        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 2,
            MaxPages = 10,
            DelayBetweenRequests = 0
        };

        // Act
        var result = await provider.TestLoadPaginatedDataAsync("test-request", config, "test-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Contains("page1-item1", result);
        Assert.Contains("page2-item2", result);
        Assert.Contains("page3-item1", result);
    }

    [Fact]
    public async Task LoadPaginatedDataAsync_WithMaxTotalItemsLimit_ShouldRespectLimit()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();
        var provider = new TestPaginationDataProvider(mockLogger);
        provider.SetupPages(new[]
        {
            new List<string> { "item1", "item2" },
            new List<string> { "item3", "item4" },
            new List<string> { "item5", "item6" }
        });

        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 2,
            MaxTotalItems = 3,
            MaxPages = 10,
            DelayBetweenRequests = 0
        };

        // Act
        var result = await provider.TestLoadPaginatedDataAsync("test-request", config, "test-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count); // Should stop after getting enough items
    }

    [Fact]
    public async Task LoadPaginatedDataAsync_WithMaxRequestsLimit_ShouldRespectLimit()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();
        var provider = new TestPaginationDataProvider(mockLogger);
        provider.SetupPages(new[]
        {
            new List<string> { "item1" },
            new List<string> { "item2" },
            new List<string> { "item3" },
            new List<string> { "item4" },
            new List<string> { "item5" }
        });

        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 1,
            MaxPages = 2,
            DelayBetweenRequests = 0
        };

        // Act
        var result = await provider.TestLoadPaginatedDataAsync("test-request", config, "test-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Should stop after 2 requests
    }

    [Fact]
    public async Task LoadPaginatedDataAsync_WithErrorResponse_ShouldReturnPartialData()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();
        var provider = new TestPaginationDataProvider(mockLogger);
        provider.SetupPages(new[]
        {
            new List<string> { "item1", "item2" }
        });
        provider.SetupError(1, "Network error");

        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 2,
            MaxPages = 5,
            DelayBetweenRequests = 0
        };

        // Act
        var result = await provider.TestLoadPaginatedDataAsync("test-request", config, "test-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Should return data from successful requests
    }

    [Fact]
    public async Task LoadPaginatedDataAsync_WithNoData_ShouldReturnNull()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();
        var provider = new TestPaginationDataProvider(mockLogger);
        provider.SetupPages(Array.Empty<List<string>>());

        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 10,
            MaxPages = 5,
            DelayBetweenRequests = 0
        };

        // Act
        var result = await provider.TestLoadPaginatedDataAsync("test-request", config, "test-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadPaginatedDataAsync_WithDefaultConfig_ShouldUseDefaults()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();
        var provider = new TestPaginationDataProvider(mockLogger);
        provider.SetupPages(new[]
        {
            new List<string> { "item1", "item2" }
        });

        // Act
        var result = await provider.TestLoadPaginatedDataAsync("test-request", null, "test-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task LoadPaginatedDataAsync_WithDelayBetweenRequests_ShouldRespectDelay()
    {
        // Arrange
        var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestPaginationDataProvider>();
        var provider = new TestPaginationDataProvider(mockLogger);
        provider.SetupPages(new[]
        {
            new List<string> { "item1" },
            new List<string> { "item2" }
        });

        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 1,
            MaxPages = 2,
            DelayBetweenRequests = 50
        };

        var startTime = DateTime.UtcNow;

        // Act
        var result = await provider.TestLoadPaginatedDataAsync("test-request", config, "test-id");

        // Assert
        var elapsed = DateTime.UtcNow - startTime;
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(elapsed.TotalMilliseconds >= 50); // Should respect delay
    }

    // Test implementation
    private class TestPaginationDataProvider : PaginationDataProviderBase<string, string>
    {
        private List<string>[] _pages = Array.Empty<List<string>>();
        private int _currentPageIndex = 0;
        private int _errorAtPage = -1;
        private string _errorMessage = string.Empty;
        private int _delay = 0;

        public TestPaginationDataProvider(ILogger logger) : base(logger)
        {
        }

        public void SetupPages(List<string>[] pages)
        {
            _pages = pages;
            _currentPageIndex = 0;
        }

        public void SetupError(int pageIndex, string errorMessage)
        {
            _errorAtPage = pageIndex;
            _errorMessage = errorMessage;
        }

        public void SetupDelay(int milliseconds)
        {
            _delay = milliseconds;
        }

        protected override async Task<PaginationResponse<string>> FetchPageAsync(
            PaginationRequest<string> request,
            CancellationToken cancellationToken)
        {
            if (_delay > 0)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            if (_currentPageIndex == _errorAtPage)
            {
                return new PaginationResponse<string>
                {
                    Data = null,
                    HasMore = false,
                    ErrorMessage = _errorMessage
                };
            }

            if (_currentPageIndex >= _pages.Length)
            {
                return new PaginationResponse<string>
                {
                    Data = new List<string>(),
                    HasMore = false
                };
            }

            var data = _pages[_currentPageIndex];
            _currentPageIndex++;

            return new PaginationResponse<string>
            {
                Data = data,
                HasMore = _currentPageIndex < _pages.Length,
                NextOffset = _currentPageIndex * request.Limit
            };
        }

        public async Task<List<string>?> TestLoadPaginatedDataAsync(
            string initialRequest,
            PaginationConfig? config = null,
            string? identifier = null,
            CancellationToken cancellationToken = default)
        {
            return await LoadPaginatedDataAsync(initialRequest, config, identifier, cancellationToken);
        }
    }
}