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
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.ApiLogging
{
    /// <summary>
    /// Integration tests for API Logging disabled scenarios.
    /// Tests that logging behaves correctly when disabled at different levels.
    /// </summary>
    public class DisabledLoggingIntegrationTests : IDisposable
    {
        private TestServer? _server;
        private HttpClient? _client;
        private DisabledTestLogWriter? _logWriter;

        private void SetupServer(bool useApiLogging, bool optionsEnabled = true)
        {
            _logWriter = new DisabledTestLogWriter();

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
                            options.UseApiLogging = useApiLogging;
                            options.ApiLoggingOptions.Enabled = optionsEnabled;
                            options.ApiLoggingOptions.ExcludedPaths = [];
                        });

                        services.AddSingleton<IApiLogWriter>(_logWriter);
                        services.AddSingleton<IApiLoggingConfigProvider, OptionsApiLoggingConfigProvider>();
                        services.AddSingleton<ISensitiveDataMasker, JsonSensitiveDataMasker>();
                        services.AddSingleton<IApiLogEntryFactory, DefaultApiLogEntryFactory>();
                        services.AddSingleton<IApiLogProcessor, SensitiveMaskProcessor>();
                        services.AddSingleton<IApiLogProcessor, TruncationProcessor>();

                        services.AddControllers(options =>
                        {
                            // Only add filter if useApiLogging is true
                            if (useApiLogging)
                            {
                                options.Filters.Add<ApiLoggingFilter>();
                            }
                        })
                        .AddApplicationPart(typeof(DisabledTestController).Assembly);
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

        #region UseApiLogging = false Tests

        [Fact]
        public async Task Get_UseApiLoggingFalse_NoLogging()
        {
            // Arrange
            SetupServer(useApiLogging: false);
            _logWriter!.Clear();

            // Act
            var response = await _client!.GetAsync("/api/disabled/data");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(_logWriter.Entries);
        }

        [Fact]
        public async Task Post_UseApiLoggingFalse_NoLogging()
        {
            // Arrange
            SetupServer(useApiLogging: false);
            _logWriter!.Clear();
            var content = new StringContent("{\"name\":\"test\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await _client!.PostAsync("/api/disabled/create", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(_logWriter.Entries);
        }

        [Fact]
        public async Task MultipleRequests_UseApiLoggingFalse_NoneLogged()
        {
            // Arrange
            SetupServer(useApiLogging: false);
            _logWriter!.Clear();

            // Act
            await _client!.GetAsync("/api/disabled/data");
            await _client.GetAsync("/api/disabled/data");
            await _client.GetAsync("/api/disabled/data");

            // Assert
            Assert.Empty(_logWriter.Entries);
        }

        #endregion

        #region Options.Enabled = false Tests

        [Fact]
        public async Task Get_OptionsEnabledFalse_NoLogging()
        {
            // Arrange
            SetupServer(useApiLogging: true, optionsEnabled: false);
            _logWriter!.Clear();

            // Act
            var response = await _client!.GetAsync("/api/disabled/data");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(_logWriter.Entries);
        }

        [Fact]
        public async Task Post_OptionsEnabledFalse_NoLogging()
        {
            // Arrange
            SetupServer(useApiLogging: true, optionsEnabled: false);
            _logWriter!.Clear();
            var content = new StringContent("{\"name\":\"test\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await _client!.PostAsync("/api/disabled/create", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(_logWriter.Entries);
        }

        #endregion

        #region Enabled = true Tests (Control Group)

        [Fact]
        public async Task Get_BothEnabled_LogsRequests()
        {
            // Arrange
            SetupServer(useApiLogging: true, optionsEnabled: true);
            _logWriter!.Clear();

            // Act
            var response = await _client!.GetAsync("/api/disabled/data");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(_logWriter.Entries);
        }

        [Fact]
        public async Task MultipleRequests_BothEnabled_AllLogged()
        {
            // Arrange
            SetupServer(useApiLogging: true, optionsEnabled: true);
            _logWriter!.Clear();

            // Act
            await _client!.GetAsync("/api/disabled/data");
            await _client.GetAsync("/api/disabled/data");
            await _client.GetAsync("/api/disabled/data");

            // Assert
            Assert.Equal(3, _logWriter.Entries.Count);
        }

        #endregion

        #region Request/Response Body Disabled Tests

        [Fact]
        public async Task Post_LogRequestBodyFalse_NoRequestBodyLogged()
        {
            // Arrange
            _logWriter = new DisabledTestLogWriter();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.Configure<MiCakeAspNetOptions>(aspNetOptions =>
                        {
                            aspNetOptions.UseApiLogging = true;
                            aspNetOptions.ApiLoggingOptions.Enabled = true;
                            aspNetOptions.ApiLoggingOptions.LogRequestBody = false;
                            aspNetOptions.ApiLoggingOptions.LogResponseBody = true;
                            aspNetOptions.ApiLoggingOptions.ExcludedPaths = [];
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
                        .AddApplicationPart(typeof(DisabledTestController).Assembly);
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

            _logWriter.Clear();
            var content = new StringContent("{\"name\":\"test\"}", Encoding.UTF8, "application/json");

            // Act
            await _client.PostAsync("/api/disabled/create", content);

            // Assert
            var entry = _logWriter.Entries.First();
            Assert.Null(entry.Request.Body);
        }

        [Fact]
        public async Task Get_LogResponseBodyFalse_NoResponseBodyLogged()
        {
            // Arrange
            _logWriter = new DisabledTestLogWriter();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.Configure<MiCakeAspNetOptions>(aspNetOptions =>
                        {
                            aspNetOptions.UseApiLogging = true;
                            aspNetOptions.ApiLoggingOptions.Enabled = true;
                            aspNetOptions.ApiLoggingOptions.LogRequestBody = true;
                            aspNetOptions.ApiLoggingOptions.LogResponseBody = false;
                            aspNetOptions.ApiLoggingOptions.ExcludedPaths = [];
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
                        .AddApplicationPart(typeof(DisabledTestController).Assembly);
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

            _logWriter.Clear();

            // Act
            await _client.GetAsync("/api/disabled/data");

            // Assert
            var entry = _logWriter.Entries.First();
            Assert.Null(entry.Response.Body);
        }

        #endregion

        #region Header Logging Tests

        [Fact]
        public async Task Get_LogHeadersFalse_NoHeadersLogged()
        {
            // Arrange
            _logWriter = new DisabledTestLogWriter();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.Configure<MiCakeAspNetOptions>(aspNetOptions =>
                        {
                            aspNetOptions.UseApiLogging = true;
                            aspNetOptions.ApiLoggingOptions.Enabled = true;
                            aspNetOptions.ApiLoggingOptions.LogRequestHeaders = false;
                            aspNetOptions.ApiLoggingOptions.LogResponseHeaders = false;
                            aspNetOptions.ApiLoggingOptions.ExcludedPaths = [];
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
                        .AddApplicationPart(typeof(DisabledTestController).Assembly);
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

            _logWriter.Clear();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/disabled/data");
            request.Headers.Add("X-Custom-Header", "test-value");

            // Act
            await _client.SendAsync(request);

            // Assert
            var entry = _logWriter.Entries.First();
            Assert.Null(entry.Request.Headers);
            Assert.Null(entry.Response.Headers);
        }

        [Fact]
        public async Task Get_LogHeadersTrue_HeadersLogged()
        {
            // Arrange
            _logWriter = new DisabledTestLogWriter();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.Configure<MiCakeAspNetOptions>(aspNetOptions =>
                        {
                            aspNetOptions.UseApiLogging = true;
                            aspNetOptions.ApiLoggingOptions.Enabled = true;
                            aspNetOptions.ApiLoggingOptions.LogRequestHeaders = true;
                            aspNetOptions.ApiLoggingOptions.LogResponseHeaders = true;
                            aspNetOptions.ApiLoggingOptions.ExcludedPaths = [];
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
                        .AddApplicationPart(typeof(DisabledTestController).Assembly);
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

            _logWriter.Clear();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/disabled/data");
            request.Headers.Add("X-Custom-Header", "test-value");

            // Act
            await _client.SendAsync(request);

            // Assert
            var entry = _logWriter.Entries.First();
            Assert.NotNull(entry.Request.Headers);
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
            _server?.Dispose();
        }
    }

    #region Test Infrastructure

    internal class DisabledTestLogWriter : IApiLogWriter
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
    [Route("api/disabled")]
    public class DisabledTestController : ControllerBase
    {
        [HttpGet("data")]
        public IActionResult GetData()
        {
            return Ok(new { Id = 1, Name = "Test Data" });
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody] object body)
        {
            return Ok(new { Created = true });
        }
    }

    #endregion
}
