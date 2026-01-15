using MiCake.AspNetCore;
using MiCake.AspNetCore.Responses;
using MiCake.AspNetCore.Responses.Internals;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Responses
{
    /// <summary>
    /// Integration tests for ResponseWrapper functionality.
    /// Tests various response types and wrapping behaviors in an ASP.NET Core context.
    /// </summary>
    public class ResponseWrapperIntegrationTests : IDisposable
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public ResponseWrapperIntegrationTests()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddLogging();

                        // Configure MiCake ASP.NET options with data wrapper enabled
                        services.Configure<MiCakeAspNetOptions>(options =>
                        {
                            options.UseDataWrapper = true;
                            options.DataWrapperOptions = new ResponseWrapperOptions();
                        });

                        services.AddControllers(options =>
                        {
                            // Manually add the wrapper filters for testing
                            options.Filters.Add<ResponseWrapperFilter>();
                            options.Filters.Add<ExceptionResponseWrapperFilter>();
                        })
                        .AddApplicationPart(typeof(ResponseWrapperTestController).Assembly);
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

        #region Normal Response Wrapping Tests

        [Fact]
        public async Task Get_ReturnsWrappedResponse_ForObjectResult()
        {
            // Act
            var response = await _client.GetAsync("/api/test/object");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            Assert.NotNull(apiResponse);
            Assert.Equal("0", apiResponse.Code);
        }

        [Fact]
        public async Task Get_ReturnsWrappedResponse_ForPrimitiveResult()
        {
            // Act
            var response = await _client.GetAsync("/api/test/primitive");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            Assert.NotNull(apiResponse);
            Assert.Equal("0", apiResponse.Code);
        }

        [Fact]
        public async Task Get_ReturnsWrappedResponse_ForListResult()
        {
            // Act
            var response = await _client.GetAsync("/api/test/list");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            Assert.NotNull(apiResponse);
            Assert.Equal("0", apiResponse.Code);
        }

        [Fact]
        public async Task Get_ReturnsWrappedResponse_ForNullResult()
        {
            // Act
            var response = await _client.GetAsync("/api/test/null");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            Assert.NotNull(apiResponse);
            Assert.Equal("0", apiResponse.Code);
            Assert.Null(apiResponse.Data);
        }

        #endregion

        #region SkipResponseWrapper Tests

        [Fact]
        public async Task Get_ReturnsUnwrappedResponse_WhenSkipAttributeOnAction()
        {
            // Act
            var response = await _client.GetAsync("/api/test/skip-action");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Should NOT be wrapped - should be raw data
            var data = JsonSerializer.Deserialize<TestData>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            Assert.NotNull(data);
            Assert.Equal("skip-action", data.Name);
            Assert.Equal(123, data.Value);
        }

        [Fact]
        public async Task Get_ReturnsUnwrappedResponse_WhenSkipAttributeOnController()
        {
            // Act
            var response = await _client.GetAsync("/api/skip-controller/data");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Should NOT be wrapped - should be raw data
            var data = JsonSerializer.Deserialize<TestData>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            Assert.NotNull(data);
            Assert.Equal("skip-controller", data.Name);
        }

        #endregion

        #region Special Return Type Tests

        [Fact]
        public async Task Get_FileResult_NotWrapped()
        {
            // Act
            var response = await _client.GetAsync("/api/test/file");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsByteArrayAsync();
            var text = Encoding.UTF8.GetString(content);
            Assert.Equal("This is a test file content.", text);
        }

        [Fact]
        public async Task Get_FileStreamResult_NotWrapped()
        {
            // Act
            var response = await _client.GetAsync("/api/test/file-stream");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Stream content here", content);
        }

        [Fact]
        public async Task Get_ContentResult_NotWrapped()
        {
            // Act
            var response = await _client.GetAsync("/api/test/content");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("<html><body>Hello World</body></html>", content);
        }

        [Fact]
        public async Task Get_RedirectResult_NotWrapped()
        {
            // Create client that doesn't follow redirects
            var client = _server.CreateClient();
            client.DefaultRequestHeaders.Clear();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/redirect");
            var response = await _server.CreateClient()
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/api/test/object", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Get_NoContentResult_NotWrapped()
        {
            // Act
            var response = await _client.GetAsync("/api/test/no-content");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Empty(content);
        }

        #endregion

        #region Ignored Status Code Tests

        [Fact]
        public async Task Get_NotFoundResult_NotWrapped_WhenInIgnoreList()
        {
            // Act
            var response = await _client.GetAsync("/api/test/not-found");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            // 404 is in default ignore list, should not be wrapped
            Assert.DoesNotContain("\"code\":", content.ToLowerInvariant());
        }

        [Fact]
        public async Task Get_CreatedResult_NotWrapped_WhenInIgnoreList()
        {
            // Act
            var response = await _client.PostAsync("/api/test/created", null);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            // 201 is in default ignore list, should not be wrapped
            Assert.DoesNotContain("\"code\":", content.ToLowerInvariant());
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task Get_ExceptionThrown_ReturnsWrappedErrorResponse()
        {
            // Act
            var response = await _client.GetAsync("/api/test/exception");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            Assert.NotNull(errorResponse);
            Assert.Equal("9999", errorResponse.Code);
            Assert.Contains("Test exception", errorResponse.Message);
        }

        [Fact]
        public async Task Get_ExceptionWithSkipAttribute_NotWrapped()
        {
            // Act & Assert - The exception should propagate as unhandled
            // since SkipResponseWrapper prevents exception wrapping
            // In TestServer, unhandled exceptions are thrown directly as InvalidOperationException
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _client.GetAsync("/api/test/exception-skip");
            });
            Assert.Contains("Test exception with skip attribute", exception.Message);
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
    /// Test data model
    /// </summary>
    public class TestData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    /// <summary>
    /// Controller for testing normal wrapping behavior
    /// </summary>
    [ApiController]
    [Route("api/test")]
    public class ResponseWrapperTestController : ControllerBase
    {
        [HttpGet("object")]
        public IActionResult GetObject()
        {
            return Ok(new TestData { Name = "test", Value = 42 });
        }

        [HttpGet("primitive")]
        public IActionResult GetPrimitive()
        {
            return Ok(12345);
        }

        [HttpGet("list")]
        public IActionResult GetList()
        {
            return Ok(new[] { "item1", "item2", "item3" });
        }

        [HttpGet("null")]
        public IActionResult GetNull()
        {
            return Ok(null);
        }

        [HttpGet("skip-action")]
        [SkipResponseWrapper]
        public IActionResult GetWithSkipAction()
        {
            return Ok(new TestData { Name = "skip-action", Value = 123 });
        }

        [HttpGet("file")]
        public IActionResult GetFile()
        {
            var bytes = Encoding.UTF8.GetBytes("This is a test file content.");
            return File(bytes, "application/octet-stream", "test.txt");
        }

        [HttpGet("file-stream")]
        public IActionResult GetFileStream()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Stream content here"));
            return File(stream, "text/plain");
        }

        [HttpGet("content")]
        public IActionResult GetContent()
        {
            return Content("<html><body>Hello World</body></html>", "text/html");
        }

        [HttpGet("redirect")]
        public IActionResult GetRedirect()
        {
            return Redirect("/api/test/object");
        }

        [HttpGet("no-content")]
        public IActionResult GetNoContent()
        {
            return NoContent();
        }

        [HttpGet("not-found")]
        public IActionResult GetNotFound()
        {
            return NotFound(new { Message = "Resource not found" });
        }

        [HttpPost("created")]
        public IActionResult PostCreated()
        {
            return Created("/api/test/object", new TestData { Name = "created", Value = 1 });
        }

        [HttpGet("exception")]
        public IActionResult GetException()
        {
            throw new InvalidOperationException("Test exception for integration testing");
        }

        [HttpGet("exception-skip")]
        [SkipResponseWrapper]
        public IActionResult GetExceptionWithSkip()
        {
            throw new InvalidOperationException("Test exception with skip attribute");
        }
    }

    /// <summary>
    /// Controller with SkipResponseWrapper at controller level
    /// </summary>
    [ApiController]
    [Route("api/skip-controller")]
    [SkipResponseWrapper]
    public class SkipWrapperController : ControllerBase
    {
        [HttpGet("data")]
        public IActionResult GetData()
        {
            return Ok(new TestData { Name = "skip-controller", Value = 456 });
        }

        [HttpGet("another")]
        public IActionResult GetAnother()
        {
            return Ok(new TestData { Name = "also-not-wrapped", Value = 789 });
        }
    }

    #endregion
}
