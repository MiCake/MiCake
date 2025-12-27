using MiCake.AspNetCore;
using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.ApiLogging
{
    /// <summary>
    /// Integration tests for API Logging status code exclusion functionality.
    /// Tests scenarios where certain status codes should be excluded from logging.
    /// </summary>
    public class StatusCodeExclusionIntegrationTests : IDisposable
    {
        private TestServer? _server;
        private HttpClient? _client;
        private InMemoryLogWriter? _logWriter;

        private void SetupServer(Action<ApiLoggingOptions>? configureOptions = null)
        {
            _logWriter = new InMemoryLogWriter();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddLogging();

                        // Configure MiCakeAspNetOptions with ApiLoggingOptions
                        services.Configure<MiCakeAspNetOptions>(aspNetOptions =>
                        {
                            aspNetOptions.UseApiLogging = true;
                            aspNetOptions.ApiLoggingOptions.Enabled = true;
                            aspNetOptions.ApiLoggingOptions.ExcludeStatusCodes = [200]; // Exclude 200 OK by default
                            aspNetOptions.ApiLoggingOptions.ExcludedPaths = [];
                            
                            // Apply custom configuration if provided
                            configureOptions?.Invoke(aspNetOptions.ApiLoggingOptions);
                        });

                        services.AddSingleton<IApiLogWriter>(_logWriter);
                        services.AddSingleton<IApiLoggingConfigProvider, OptionsApiLoggingConfigProvider>();
                        services.AddSingleton<ISensitiveDataMasker, JsonSensitiveDataMasker>();
                        services.AddSingleton<IApiLogEntryFactory, DefaultApiLogEntryFactory>();
                        services.AddSingleton<IApiLogProcessor, SensitiveMaskProcessor>();
                        services.AddSingleton<IApiLogProcessor, TruncationProcessor>();

                        services.AddControllers(options =>
                        {
                            options.Filters.Add<ApiLoggingFilter>();
                        })
                        .AddApplicationPart(typeof(StatusCodeTestController).Assembly);
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

        #region Status Code Exclusion Tests

        [Fact]
        public async Task Get_ExcludedStatusCode200_NotLogged()
        {
            // Arrange
            SetupServer();
            _logWriter!.Clear();

            // Act
            var response = await _client!.GetAsync("/api/status/ok");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(_logWriter.Entries);
        }

        [Fact]
        public async Task Get_NonExcludedStatusCode404_IsLogged()
        {
            // Arrange
            SetupServer();
            _logWriter!.Clear();

            // Act
            var response = await _client!.GetAsync("/api/status/not-found");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Single(_logWriter.Entries);
            Assert.Equal(404, _logWriter.Entries.First().Response.StatusCode);
        }

        [Fact]
        public async Task Get_NonExcludedStatusCode500_IsLogged()
        {
            // Arrange
            SetupServer();
            _logWriter!.Clear();

            // Act
            var response = await _client!.GetAsync("/api/status/server-error");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Single(_logWriter.Entries);
            Assert.Equal(500, _logWriter.Entries.First().Response.StatusCode);
        }

        [Fact]
        public async Task Get_MultipleExcludedStatusCodes_NoneLogged()
        {
            // Arrange
            SetupServer(options =>
            {
                options.ExcludeStatusCodes = [200, 201, 204];
            });
            _logWriter!.Clear();

            // Act
            var response1 = await _client!.GetAsync("/api/status/ok");
            var response2 = await _client.PostAsync("/api/status/created", null);
            var response3 = await _client.DeleteAsync("/api/status/no-content");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, response3.StatusCode);
            Assert.Empty(_logWriter.Entries);
        }

        [Fact]
        public async Task Get_Exclude4xxOnly_Logs200And5xx()
        {
            // Arrange
            SetupServer(options =>
            {
                options.ExcludeStatusCodes = [400, 401, 403, 404];
            });
            _logWriter!.Clear();

            // Act
            var ok = await _client!.GetAsync("/api/status/ok");
            var notFound = await _client.GetAsync("/api/status/not-found");
            var error = await _client.GetAsync("/api/status/server-error");

            // Assert
            Assert.Equal(2, _logWriter.Entries.Count);
            Assert.Contains(_logWriter.Entries, e => e.Response.StatusCode == 200);
            Assert.Contains(_logWriter.Entries, e => e.Response.StatusCode == 500);
        }

        [Fact]
        public async Task Get_EmptyExclusionList_LogsAllStatusCodes()
        {
            // Arrange
            SetupServer(options =>
            {
                options.ExcludeStatusCodes = [];
            });
            _logWriter!.Clear();

            // Act
            await _client!.GetAsync("/api/status/ok");
            await _client.GetAsync("/api/status/not-found");
            await _client.GetAsync("/api/status/bad-request");

            // Assert
            Assert.Equal(3, _logWriter.Entries.Count);
        }

        #endregion

        #region AlwaysLog Attribute Tests

        [Fact]
        public async Task Get_AlwaysLogAttribute_OverridesExclusion()
        {
            // Arrange
            SetupServer(options =>
            {
                options.ExcludeStatusCodes = [200];
            });
            _logWriter!.Clear();

            // Act
            var response = await _client!.GetAsync("/api/status/always-logged");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(_logWriter.Entries);
        }

        [Fact]
        public async Task Get_AlwaysLogWithExcludedStatusCodes_StillLogs()
        {
            // Arrange
            SetupServer(options =>
            {
                options.ExcludeStatusCodes = [200, 201, 204];
            });
            _logWriter!.Clear();

            // Act
            var response = await _client!.GetAsync("/api/status/always-logged");

            // Assert
            Assert.Single(_logWriter.Entries);
            Assert.Equal(200, _logWriter.Entries.First().Response.StatusCode);
        }

        #endregion

        #region Mixed Scenarios

        [Fact]
        public async Task MixedRequests_OnlyNonExcludedAreLogged()
        {
            // Arrange
            SetupServer(options =>
            {
                options.ExcludeStatusCodes = [200, 204];
            });
            _logWriter!.Clear();

            // Act
            await _client!.GetAsync("/api/status/ok");          // 200 - excluded
            await _client.GetAsync("/api/status/not-found");    // 404 - logged
            await _client.DeleteAsync("/api/status/no-content");// 204 - excluded
            await _client.GetAsync("/api/status/bad-request");  // 400 - logged
            await _client.GetAsync("/api/status/always-logged");// 200 - logged (AlwaysLog)

            // Assert
            Assert.Equal(3, _logWriter.Entries.Count);
            Assert.Contains(_logWriter.Entries, e => e.Response.StatusCode == 404);
            Assert.Contains(_logWriter.Entries, e => e.Response.StatusCode == 400);
            Assert.Contains(_logWriter.Entries, e => e.Response.StatusCode == 200); // AlwaysLog
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
            _server?.Dispose();
        }
    }

    #region Test Infrastructure

    internal class InMemoryLogWriter : IApiLogWriter
    {
        private readonly ConcurrentBag<ApiLogEntry> _entries = new();

        public IReadOnlyCollection<ApiLogEntry> Entries => _entries.ToList().AsReadOnly();

        public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public void Clear()
        {
            while (_entries.TryTake(out _)) { }
        }
    }

    [ApiController]
    [Route("api/status")]
    public class StatusCodeTestController : ControllerBase
    {
        [HttpGet("ok")]
        public IActionResult GetOk()
        {
            return Ok(new { Status = "OK" });
        }

        [HttpGet("not-found")]
        public IActionResult GetNotFound()
        {
            return NotFound(new { Error = "Not found" });
        }

        [HttpGet("bad-request")]
        public IActionResult GetBadRequest()
        {
            return BadRequest(new { Error = "Bad request" });
        }

        [HttpGet("server-error")]
        public IActionResult GetServerError()
        {
            return StatusCode(500, new { Error = "Server error" });
        }

        [HttpPost("created")]
        public IActionResult PostCreated()
        {
            return Created("/api/status/1", new { Id = 1 });
        }

        [HttpDelete("no-content")]
        public IActionResult DeleteNoContent()
        {
            return NoContent();
        }

        [HttpGet("always-logged")]
        [AlwaysLog]
        public IActionResult GetAlwaysLogged()
        {
            return Ok(new { Critical = "Always logged" });
        }
    }

    #endregion
}
