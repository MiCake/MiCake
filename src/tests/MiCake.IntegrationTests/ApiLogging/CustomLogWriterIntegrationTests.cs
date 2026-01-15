using MiCake.AspNetCore;
using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.ApiLogging
{
    /// <summary>
    /// Integration tests for custom API log writer implementations.
    /// Tests that custom implementations are properly invoked and receive correct data.
    /// </summary>
    public class CustomLogWriterIntegrationTests : IDisposable
    {
        private TestServer? _server;
        private HttpClient? _client;

        private void SetupServer<TLogWriter>(TLogWriter logWriter) where TLogWriter : class, IApiLogWriter
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddLogging();

                        // Configure MiCakeAspNetOptions with ApiLoggingOptions
                        services.Configure<MiCakeAspNetOptions>(options =>
                        {
                            options.UseApiLogging = true;
                            options.ApiLoggingOptions.Enabled = true;
                            options.ApiLoggingOptions.ExcludedPaths = [];
                        });

                        services.AddSingleton<IApiLogWriter>(logWriter);
                        services.AddSingleton<IApiLoggingConfigProvider, OptionsApiLoggingConfigProvider>();
                        services.AddSingleton<ISensitiveDataMasker, JsonSensitiveDataMasker>();
                        services.AddSingleton<IApiLogEntryFactory, DefaultApiLogEntryFactory>();
                        services.AddSingleton<IApiLogProcessor, SensitiveMaskProcessor>();
                        services.AddSingleton<IApiLogProcessor, TruncationProcessor>();

                        services.AddControllers(options =>
                        {
                            options.Filters.Add<ApiLoggingFilter>();
                        })
                        .AddApplicationPart(typeof(CustomWriterTestController).Assembly);
                    });
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                });

            var host = hostBuilder.Start();
            _server = host.GetTestServer();
            _client = _server.CreateClient();
        }

        #region Custom Writer Invocation Tests

        [Fact]
        public async Task Get_CustomWriter_IsInvoked()
        {
            // Arrange
            var customWriter = new TrackingLogWriter();
            SetupServer(customWriter);

            // Act
            await _client!.GetAsync("/api/custom-writer/data");

            // Assert
            Assert.Equal(1, customWriter.WriteCount);
        }

        [Fact]
        public async Task MultipleRequests_CustomWriter_InvokedForEach()
        {
            // Arrange
            var customWriter = new TrackingLogWriter();
            SetupServer(customWriter);

            // Act
            await _client!.GetAsync("/api/custom-writer/data");
            await _client.GetAsync("/api/custom-writer/data");
            await _client.GetAsync("/api/custom-writer/data");

            // Assert
            Assert.Equal(3, customWriter.WriteCount);
        }

        [Fact]
        public async Task Get_CustomWriter_ReceivesCompleteEntry()
        {
            // Arrange
            var customWriter = new DetailedLogWriter();
            SetupServer(customWriter);

            // Act
            await _client!.GetAsync("/api/custom-writer/data?param=value");

            // Assert
            Assert.Single(customWriter.Entries);
            var entry = customWriter.Entries.First();

            // Verify all expected fields are populated
            Assert.False(string.IsNullOrEmpty(entry.CorrelationId));
            Assert.True(entry.Timestamp != default);
            Assert.Equal("GET", entry.Request.Method);
            Assert.Equal("/api/custom-writer/data", entry.Request.Path);
            Assert.Equal("?param=value", entry.Request.QueryString);
            Assert.Equal(200, entry.Response.StatusCode);
            Assert.True(entry.ElapsedMilliseconds >= 0);
        }

        #endregion

        #region Async Writer Tests

        [Fact]
        public async Task Get_AsyncWriter_CompletesSuccessfully()
        {
            // Arrange
            var asyncWriter = new AsyncDelayLogWriter(delayMs: 10);
            SetupServer(asyncWriter);

            // Act
            var response = await _client!.GetAsync("/api/custom-writer/data");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, asyncWriter.WriteCount);
        }

        [Fact]
        public async Task ConcurrentRequests_AsyncWriter_HandlesAllRequests()
        {
            // Arrange
            var asyncWriter = new AsyncDelayLogWriter(delayMs: 5);
            SetupServer(asyncWriter);

            // Act
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client!.GetAsync($"/api/custom-writer/data?id={i}"));
            }
            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(10, asyncWriter.WriteCount);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Get_WriterThrowsException_RequestStillCompletes()
        {
            // Arrange
            var failingWriter = new FailingLogWriter();
            SetupServer(failingWriter);

            // Act
            var response = await _client!.GetAsync("/api/custom-writer/data");

            // Assert
            // Request should complete successfully even if writer fails
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, failingWriter.AttemptCount);
        }

        [Fact]
        public async Task MultipleRequests_WriterThrowsException_AllRequestsComplete()
        {
            // Arrange
            var failingWriter = new FailingLogWriter();
            SetupServer(failingWriter);

            // Act
            var responses = new List<HttpResponseMessage>();
            for (int i = 0; i < 3; i++)
            {
                responses.Add(await _client!.GetAsync("/api/custom-writer/data"));
            }

            // Assert
            Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
            Assert.Equal(3, failingWriter.AttemptCount);
        }

        #endregion

        #region Entry Content Verification Tests

        [Fact]
        public async Task Post_WriterReceivesRequestMetadata()
        {
            // Arrange
            var detailedWriter = new DetailedLogWriter();
            SetupServer(detailedWriter);
            var requestBody = JsonSerializer.Serialize(new { Name = "Test", Value = 42 });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Act
            await _client!.PostAsync("/api/custom-writer/echo", content);

            // Assert
            var entry = detailedWriter.Entries.First();
            Assert.NotNull(entry.Request);
            Assert.Equal("POST", entry.Request.Method);
            Assert.Contains("/api/custom-writer/echo", entry.Request.Path);
            // Note: Request body may be empty if consumed by model binding before filter can read it
        }

        [Fact]
        public async Task Get_WriterReceivesResponseBody()
        {
            // Arrange
            var detailedWriter = new DetailedLogWriter();
            SetupServer(detailedWriter);

            // Act
            await _client!.GetAsync("/api/custom-writer/data");

            // Assert
            var entry = detailedWriter.Entries.First();
            Assert.NotNull(entry.Response.Body);
            Assert.Contains("TestData", entry.Response.Body);
        }

        [Fact]
        public async Task Get_DifferentStatusCodes_WriterReceivesCorrectStatus()
        {
            // Arrange
            var detailedWriter = new DetailedLogWriter();
            SetupServer(detailedWriter);

            // Act
            await _client!.GetAsync("/api/custom-writer/data");      // 200
            await _client.GetAsync("/api/custom-writer/not-found");  // 404
            await _client.PostAsync("/api/custom-writer/created", null); // 201

            // Assert
            Assert.Equal(3, detailedWriter.Entries.Count);
            Assert.Contains(detailedWriter.Entries, e => e.Response.StatusCode == 200);
            Assert.Contains(detailedWriter.Entries, e => e.Response.StatusCode == 404);
            Assert.Contains(detailedWriter.Entries, e => e.Response.StatusCode == 201);
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
            _server?.Dispose();
        }
    }

    #region Test Infrastructure

    /// <summary>
    /// Simple log writer that tracks invocation count.
    /// </summary>
    internal class TrackingLogWriter : IApiLogWriter
    {
        private int _writeCount;

        public int WriteCount => _writeCount;

        public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _writeCount);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Log writer that stores all entries for detailed verification.
    /// </summary>
    internal class DetailedLogWriter : IApiLogWriter
    {
        private readonly ConcurrentBag<ApiLogEntry> _entries = new();

        public IReadOnlyCollection<ApiLogEntry> Entries => _entries.ToList().AsReadOnly();

        public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Log writer that simulates async delay.
    /// </summary>
    internal class AsyncDelayLogWriter : IApiLogWriter
    {
        private readonly int _delayMs;
        private int _writeCount;

        public int WriteCount => _writeCount;

        public AsyncDelayLogWriter(int delayMs)
        {
            _delayMs = delayMs;
        }

        public async Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delayMs, cancellationToken);
            Interlocked.Increment(ref _writeCount);
        }
    }

    /// <summary>
    /// Log writer that always throws an exception.
    /// </summary>
    internal class FailingLogWriter : IApiLogWriter
    {
        private int _attemptCount;

        public int AttemptCount => _attemptCount;

        public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _attemptCount);
            throw new InvalidOperationException("Simulated log write failure");
        }
    }

    [ApiController]
    [Route("api/custom-writer")]
    public class CustomWriterTestController : ControllerBase
    {
        [HttpGet("data")]
        public IActionResult GetData()
        {
            return Ok(new { Id = 1, Name = "TestData" });
        }

        [HttpPost("echo")]
        public IActionResult Echo([FromBody] object body)
        {
            return Ok(body);
        }

        [HttpGet("not-found")]
        public IActionResult GetNotFound()
        {
            return NotFound();
        }

        [HttpPost("created")]
        public IActionResult Create()
        {
            return Created("/api/custom-writer/1", new { Id = 1 });
        }
    }

    #endregion
}
