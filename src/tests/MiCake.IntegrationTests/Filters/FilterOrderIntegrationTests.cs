using MiCake.AspNetCore;
using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using MiCake.AspNetCore.Uow;
using MiCake.DDD.Uow;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Filters
{
    /// <summary>
    /// Integration tests to verify that ApiLoggingFilter and UnitOfWorkFilter
    /// run at the correct order in the filter pipeline.
    /// Both filters implement IOrderedFilter with Order = int.MaxValue to ensure they run last.
    /// </summary>
    public class FilterOrderIntegrationTests : IDisposable
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly FilterExecutionTracker _tracker;
        private readonly TestLogWriter _logWriter;

        public FilterOrderIntegrationTests()
        {
            _tracker = new FilterExecutionTracker();
            _logWriter = new TestLogWriter();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.AddSingleton(_tracker);
                        services.AddSingleton<IApiLogWriter>(_logWriter);

                        // Configure MiCakeAspNetOptions
                        services.Configure<MiCakeAspNetOptions>(options =>
                        {
                            options.UseApiLogging = true;
                            options.ApiLoggingOptions.Enabled = true;
                            options.UnitOfWork.EnableAutoUnitOfWork = true;
                        });

                        // Register API logging services
                        services.AddSingleton<IApiLoggingConfigProvider, OptionsApiLoggingConfigProvider>();
                        services.AddSingleton<ISensitiveDataMasker, JsonSensitiveDataMasker>();
                        services.AddSingleton<IApiLogEntryFactory, DefaultApiLogEntryFactory>();

                        // Register mock UnitOfWorkManager
                        services.AddScoped<IUnitOfWorkManager, TestMockUnitOfWorkManager>();

                        services.AddControllers(options =>
                        {
                            // Add tracking filters at different order positions
                            options.Filters.Add<LowOrderFilter>(); // Order = 0
                            options.Filters.Add<MidOrderFilter>(); // Order = 100
                            options.Filters.Add<HighOrderFilter>(); // Order = 1000

                            // Add tracking wrappers for MiCake filters
                            options.Filters.Add<TrackingApiLoggingFilter>();
                            options.Filters.Add<TrackingUnitOfWorkFilter>();
                        })
                        .AddApplicationPart(typeof(FilterOrderTestController).Assembly);
                    });
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });

            var host = hostBuilder.Start();
            _server = host.GetTestServer();
            _client = _server.CreateClient();
        }

        public void Dispose()
        {
            _client?.Dispose();
            _server?.Dispose();
        }

        #region Filter Order Tests

        [Fact]
        public async Task ApiLoggingFilter_ShouldRunAfterAllOtherFilters()
        {
            // Arrange
            _tracker.Clear();

            // Act
            var response = await _client.GetAsync("/api/filtertest/test");
            response.EnsureSuccessStatusCode();

            // Assert - Verify execution order
            var executionOrder = _tracker.GetExecutionOrder();

            // Find indices
            int lowOrderIndex = executionOrder.FindIndex(x => x == nameof(LowOrderFilter));
            int midOrderIndex = executionOrder.FindIndex(x => x == nameof(MidOrderFilter));
            int highOrderIndex = executionOrder.FindIndex(x => x == nameof(HighOrderFilter));
            int apiLoggingIndex = executionOrder.FindIndex(x => x == nameof(ApiLoggingFilter));

            // ApiLoggingFilter should run after all other filters (higher index = later execution)
            Assert.True(apiLoggingIndex > lowOrderIndex,
                $"ApiLoggingFilter (index {apiLoggingIndex}) should run after LowOrderFilter (index {lowOrderIndex})");
            Assert.True(apiLoggingIndex > midOrderIndex,
                $"ApiLoggingFilter (index {apiLoggingIndex}) should run after MidOrderFilter (index {midOrderIndex})");
            Assert.True(apiLoggingIndex > highOrderIndex,
                $"ApiLoggingFilter (index {apiLoggingIndex}) should run after HighOrderFilter (index {highOrderIndex})");
        }

        [Fact]
        public async Task UnitOfWorkFilter_ShouldRunAfterAllOtherActionFilters()
        {
            // Arrange
            _tracker.Clear();

            // Act
            var response = await _client.GetAsync("/api/filtertest/test");
            response.EnsureSuccessStatusCode();

            // Assert - Verify execution order for action filters
            var executionOrder = _tracker.GetExecutionOrder();

            int lowOrderIndex = executionOrder.FindIndex(x => x == nameof(LowOrderFilter));
            int midOrderIndex = executionOrder.FindIndex(x => x == nameof(MidOrderFilter));
            int highOrderIndex = executionOrder.FindIndex(x => x == nameof(HighOrderFilter));
            int uowIndex = executionOrder.FindIndex(x => x == nameof(UnitOfWorkFilter));

            // UnitOfWorkFilter should run after all other filters
            Assert.True(uowIndex > lowOrderIndex,
                $"UnitOfWorkFilter (index {uowIndex}) should run after LowOrderFilter (index {lowOrderIndex})");
            Assert.True(uowIndex > midOrderIndex,
                $"UnitOfWorkFilter (index {uowIndex}) should run after MidOrderFilter (index {midOrderIndex})");
            Assert.True(uowIndex > highOrderIndex,
                $"UnitOfWorkFilter (index {uowIndex}) should run after HighOrderFilter (index {highOrderIndex})");
        }

        [Fact]
        public void BothMiCakeFilters_ShouldHaveHighestOrderValue()
        {
            // Arrange & Act
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IApiLogWriter>(_logWriter);
            services.Configure<MiCakeAspNetOptions>(options =>
            {
                options.UseApiLogging = true;
                options.ApiLoggingOptions.Enabled = true;
            });
            services.AddSingleton<IApiLoggingConfigProvider, OptionsApiLoggingConfigProvider>();
            services.AddSingleton<ISensitiveDataMasker, JsonSensitiveDataMasker>();
            services.AddSingleton<IApiLogEntryFactory, DefaultApiLogEntryFactory>();
            services.AddScoped<IUnitOfWorkManager, TestMockUnitOfWorkManager>();

            var provider = services.BuildServiceProvider();

            var apiLoggingFilter = new ApiLoggingFilter(
                provider.GetRequiredService<IApiLoggingConfigProvider>(),
                provider.GetRequiredService<IApiLogEntryFactory>(),
                Array.Empty<IApiLogProcessor>(),
                provider.GetRequiredService<IApiLogWriter>(),
                NullLogger<ApiLoggingFilter>.Instance);

            var uowFilter = new UnitOfWorkFilter(
                provider.GetRequiredService<IUnitOfWorkManager>(),
                provider.GetRequiredService<IOptions<MiCakeAspNetOptions>>(),
                NullLogger<UnitOfWorkFilter>.Instance);

            // Assert - Both should have int.MaxValue order
            Assert.Equal(int.MaxValue, apiLoggingFilter.Order);
            Assert.Equal(int.MaxValue, uowFilter.Order);
        }

        [Fact]
        public async Task FilterPipeline_ShouldMaintainCorrectOrderWithMultipleRequests()
        {
            // Arrange
            _tracker.Clear();

            // Act - Make multiple requests
            for (int i = 0; i < 5; i++)
            {
                await _client.GetAsync("/api/filtertest/test");
            }

            // Assert - Each request should have consistent order
            var allExecutions = _tracker.GetAllExecutions();

            foreach (var execution in allExecutions)
            {
                int lowOrderIndex = execution.FindIndex(x => x == nameof(LowOrderFilter));
                int apiLoggingIndex = execution.FindIndex(x => x == nameof(ApiLoggingFilter));
                int uowIndex = execution.FindIndex(x => x == nameof(UnitOfWorkFilter));

                // MiCake filters should always run after LowOrderFilter
                Assert.True(apiLoggingIndex > lowOrderIndex,
                    "ApiLoggingFilter should consistently run after LowOrderFilter");
                Assert.True(uowIndex > lowOrderIndex,
                    "UnitOfWorkFilter should consistently run after LowOrderFilter");
            }
        }

        #endregion
    }

    #region Test Infrastructure

    /// <summary>
    /// Tracks filter execution order across requests
    /// </summary>
    internal class FilterExecutionTracker
    {
        private readonly ConcurrentBag<List<string>> _allExecutions = new();
        private readonly AsyncLocal<List<string>> _currentRequest = new();

        public void RecordExecution(string filterName)
        {
            if (_currentRequest.Value == null)
            {
                _currentRequest.Value = new List<string>();
                _allExecutions.Add(_currentRequest.Value);
            }
            _currentRequest.Value.Add(filterName);
        }

        public List<string> GetExecutionOrder()
        {
            return _allExecutions.FirstOrDefault() ?? new List<string>();
        }

        public List<List<string>> GetAllExecutions()
        {
            return _allExecutions.ToList();
        }

        public void Clear()
        {
            _allExecutions.Clear();
            _currentRequest.Value = null;
        }
    }

    /// <summary>
    /// Low order filter (runs first)
    /// </summary>
    internal class LowOrderFilter : IAsyncActionFilter, IAsyncResourceFilter, IOrderedFilter
    {
        private readonly FilterExecutionTracker _tracker;

        public int Order => 0;

        public LowOrderFilter(FilterExecutionTracker tracker)
        {
            _tracker = tracker;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _tracker.RecordExecution(nameof(LowOrderFilter));
            await next();
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            _tracker.RecordExecution(nameof(LowOrderFilter));
            await next();
        }
    }

    /// <summary>
    /// Mid order filter
    /// </summary>
    internal class MidOrderFilter : IAsyncActionFilter, IAsyncResourceFilter, IOrderedFilter
    {
        private readonly FilterExecutionTracker _tracker;

        public int Order => 100;

        public MidOrderFilter(FilterExecutionTracker tracker)
        {
            _tracker = tracker;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _tracker.RecordExecution(nameof(MidOrderFilter));
            await next();
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            _tracker.RecordExecution(nameof(MidOrderFilter));
            await next();
        }
    }

    /// <summary>
    /// High order filter
    /// </summary>
    internal class HighOrderFilter : IAsyncActionFilter, IAsyncResourceFilter, IOrderedFilter
    {
        private readonly FilterExecutionTracker _tracker;

        public int Order => 1000;

        public HighOrderFilter(FilterExecutionTracker tracker)
        {
            _tracker = tracker;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _tracker.RecordExecution(nameof(HighOrderFilter));
            await next();
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            _tracker.RecordExecution(nameof(HighOrderFilter));
            await next();
        }
    }

    /// <summary>
    /// Tracking wrapper for ApiLoggingFilter that records execution and delegates to the real filter
    /// </summary>
    internal class TrackingApiLoggingFilter : IAsyncResourceFilter, IAsyncResultFilter, IOrderedFilter
    {
        private readonly FilterExecutionTracker _tracker;
        private readonly ApiLoggingFilter _innerFilter;

        public int Order => int.MaxValue; // Same as ApiLoggingFilter

        public TrackingApiLoggingFilter(
            FilterExecutionTracker tracker,
            IApiLoggingConfigProvider configProvider,
            IApiLogEntryFactory entryFactory,
            IEnumerable<IApiLogProcessor> processors,
            IApiLogWriter logWriter,
            ILogger<ApiLoggingFilter> logger)
        {
            _tracker = tracker;
            _innerFilter = new ApiLoggingFilter(configProvider, entryFactory, processors, logWriter, logger);
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            _tracker.RecordExecution(nameof(ApiLoggingFilter));
            await _innerFilter.OnResourceExecutionAsync(context, next);
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await _innerFilter.OnResultExecutionAsync(context, next);
        }
    }

    /// <summary>
    /// Tracking wrapper for UnitOfWorkFilter that records execution and delegates to the real filter
    /// </summary>
    internal class TrackingUnitOfWorkFilter : IAsyncActionFilter, IOrderedFilter
    {
        private readonly FilterExecutionTracker _tracker;
        private readonly UnitOfWorkFilter _innerFilter;

        public int Order => int.MaxValue; // Same as UnitOfWorkFilter

        public TrackingUnitOfWorkFilter(
            FilterExecutionTracker tracker,
            IUnitOfWorkManager unitOfWorkManager,
            IOptions<MiCakeAspNetOptions> aspnetOptions,
            ILogger<UnitOfWorkFilter> logger)
        {
            _tracker = tracker;
            _innerFilter = new UnitOfWorkFilter(unitOfWorkManager, aspnetOptions, logger);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _tracker.RecordExecution(nameof(UnitOfWorkFilter));
            await _innerFilter.OnActionExecutionAsync(context, next);
        }
    }

    /// <summary>
    /// Mock UnitOfWorkManager for testing
    /// </summary>
    internal class TestMockUnitOfWorkManager : IUnitOfWorkManager
    {
        public IUnitOfWork? Current => null;

        public Task<IUnitOfWork> BeginAsync(bool requiresNew = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IUnitOfWork>(new TestMockUnitOfWork());
        }

        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, bool requiresNew = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IUnitOfWork>(new TestMockUnitOfWork());
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Mock UnitOfWork for testing
    /// </summary>
    internal class TestMockUnitOfWork : IUnitOfWork
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsDisposed => false;
        public bool IsCompleted => false;
        public bool HasActiveTransactions => false;
        public IsolationLevel? IsolationLevel => null;
        public IUnitOfWork? Parent => null;

#pragma warning disable CS0067 // Event is never used - required by interface
        public event EventHandler<UnitOfWorkEventArgs>? OnCommitting;
        public event EventHandler<UnitOfWorkEventArgs>? OnCommitted;
        public event EventHandler<UnitOfWorkEventArgs>? OnRollingBack;
        public event EventHandler<UnitOfWorkEventArgs>? OnRolledBack;
#pragma warning restore CS0067

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkAsCompletedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string> CreateSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(name);
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    /// <summary>
    /// In-memory log writer for testing
    /// </summary>
    internal class TestLogWriter : IApiLogWriter
    {
        private readonly ConcurrentBag<ApiLogEntry> _entries = new();

        public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public IReadOnlyList<ApiLogEntry> GetEntries() => _entries.ToList();
        public void Clear() => _entries.Clear();
    }

    /// <summary>
    /// Test controller for filter order testing
    /// </summary>
    [ApiController]
    [Route("api/filtertest")]
    public class FilterOrderTestController : ControllerBase
    {
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "success" });
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody] object data)
        {
            return Ok(new { created = true });
        }
    }

    #endregion
}
