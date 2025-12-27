using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="DefaultApiLogEntryFactory"/> implementation.
    /// </summary>
    public class DefaultApiLogEntryFactory_Tests
    {
        private readonly DefaultApiLogEntryFactory _factory;

        public DefaultApiLogEntryFactory_Tests()
        {
            _factory = new DefaultApiLogEntryFactory();
        }

        #region CreateEntry Tests

        [Fact]
        public void CreateEntry_SetsCorrelationId()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Equal(httpContext.TraceIdentifier, entry.CorrelationId);
        }

        [Fact]
        public void CreateEntry_SetsTimestamp()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var config = CreateConfig();
            var beforeCreate = DateTimeOffset.UtcNow;

            // Act
            var entry = _factory.CreateEntry(httpContext, config);
            var afterCreate = DateTimeOffset.UtcNow;

            // Assert
            Assert.InRange(entry.Timestamp, beforeCreate, afterCreate);
        }

        [Fact]
        public void CreateEntry_SetsRequestMethod()
        {
            // Arrange
            var httpContext = CreateHttpContext(method: "POST");
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Equal("POST", entry.Request.Method);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public void CreateEntry_SetsRequestMethod_AllMethods(string method)
        {
            // Arrange
            var httpContext = CreateHttpContext(method: method);
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Equal(method, entry.Request.Method);
        }

        [Fact]
        public void CreateEntry_SetsRequestPath()
        {
            // Arrange
            var httpContext = CreateHttpContext(path: "/api/users/123");
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Equal("/api/users/123", entry.Request.Path);
        }

        [Fact]
        public void CreateEntry_SetsQueryString()
        {
            // Arrange
            var httpContext = CreateHttpContext(queryString: "?page=1&size=10");
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Equal("?page=1&size=10", entry.Request.QueryString);
        }

        [Fact]
        public void CreateEntry_NoQueryString_ReturnsNull()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Null(entry.Request.QueryString);
        }

        [Fact]
        public void CreateEntry_SetsContentType()
        {
            // Arrange
            var httpContext = CreateHttpContext(contentType: "application/json");
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Equal("application/json", entry.Request.ContentType);
        }

        [Fact]
        public void CreateEntry_SetsContentLength()
        {
            // Arrange
            var httpContext = CreateHttpContext(contentLength: 1234);
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Equal(1234, entry.Request.ContentLength);
        }

        [Fact]
        public void CreateEntry_LogRequestHeadersEnabled_CapturesHeaders()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Request.Headers["X-Custom-Header"] = "custom-value";
            httpContext.Request.Headers["Accept"] = "application/json";
            var config = CreateConfig(logRequestHeaders: true);

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.NotNull(entry.Request.Headers);
            Assert.True(entry.Request.Headers.Count > 0);
        }

        [Fact]
        public void CreateEntry_LogRequestHeadersDisabled_NoHeaders()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Request.Headers["X-Custom-Header"] = "custom-value";
            var config = CreateConfig(logRequestHeaders: false);

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.Null(entry.Request.Headers);
        }

        [Fact]
        public void CreateEntry_SensitiveHeaders_AreMasked()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer secret-token";
            httpContext.Request.Headers["X-Api-Key"] = "my-api-key";
            var config = CreateConfig(logRequestHeaders: true);

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.NotNull(entry.Request.Headers);
            Assert.Equal("***", entry.Request.Headers["Authorization"]);
        }

        [Fact]
        public void CreateEntry_CookieHeader_IsMasked()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Request.Headers["Cookie"] = "session=abc123";
            var config = CreateConfig(logRequestHeaders: true);

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.NotNull(entry.Request.Headers);
            Assert.Equal("***", entry.Request.Headers["Cookie"]);
        }

        [Fact]
        public void CreateEntry_ResponseInitialized()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var config = CreateConfig();

            // Act
            var entry = _factory.CreateEntry(httpContext, config);

            // Assert
            Assert.NotNull(entry.Response);
        }

        #endregion

        #region PopulateResponse Tests

        [Fact]
        public void PopulateResponse_SetsStatusCode()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Response.StatusCode = 201;
            var config = CreateConfig();
            var entry = _factory.CreateEntry(httpContext, config);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, TimeSpan.Zero);

            // Assert
            Assert.Equal(201, entry.Response.StatusCode);
        }

        [Theory]
        [InlineData(200)]
        [InlineData(201)]
        [InlineData(204)]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(404)]
        [InlineData(500)]
        public void PopulateResponse_SetsStatusCode_AllStatusCodes(int statusCode)
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Response.StatusCode = statusCode;
            var config = CreateConfig();
            var entry = _factory.CreateEntry(httpContext, config);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, TimeSpan.Zero);

            // Assert
            Assert.Equal(statusCode, entry.Response.StatusCode);
        }

        [Fact]
        public void PopulateResponse_SetsResponseBody()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var config = CreateConfig();
            var entry = _factory.CreateEntry(httpContext, config);
            var responseBody = """{"id": 123}""";

            // Act
            _factory.PopulateResponse(entry, httpContext, config, responseBody, TimeSpan.Zero);

            // Assert
            Assert.Equal(responseBody, entry.Response.Body);
        }

        [Fact]
        public void PopulateResponse_NullResponseBody_SetsNull()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var config = CreateConfig();
            var entry = _factory.CreateEntry(httpContext, config);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, TimeSpan.Zero);

            // Assert
            Assert.Null(entry.Response.Body);
        }

        [Fact]
        public void PopulateResponse_SetsElapsedMilliseconds()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var config = CreateConfig();
            var entry = _factory.CreateEntry(httpContext, config);
            var elapsed = TimeSpan.FromMilliseconds(150);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, elapsed);

            // Assert
            Assert.Equal(150, entry.ElapsedMilliseconds);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public void PopulateResponse_SetsElapsedMilliseconds_VariousTimes(long ms)
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var config = CreateConfig();
            var entry = _factory.CreateEntry(httpContext, config);
            var elapsed = TimeSpan.FromMilliseconds(ms);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, elapsed);

            // Assert
            Assert.Equal(ms, entry.ElapsedMilliseconds);
        }

        [Fact]
        public void PopulateResponse_SetsResponseContentType()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Response.ContentType = "application/json; charset=utf-8";
            var config = CreateConfig();
            var entry = _factory.CreateEntry(httpContext, config);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, TimeSpan.Zero);

            // Assert
            Assert.Equal("application/json; charset=utf-8", entry.Response.ContentType);
        }

        [Fact]
        public void PopulateResponse_SetsResponseContentLength()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Response.ContentLength = 5678;
            var config = CreateConfig();
            var entry = _factory.CreateEntry(httpContext, config);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, TimeSpan.Zero);

            // Assert
            Assert.Equal(5678, entry.Response.ContentLength);
        }

        [Fact]
        public void PopulateResponse_LogResponseHeadersEnabled_CapturesHeaders()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Response.Headers["X-Request-Id"] = "abc123";
            httpContext.Response.Headers["X-Custom"] = "value";
            var config = CreateConfig(logResponseHeaders: true);
            var entry = _factory.CreateEntry(httpContext, config);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, TimeSpan.Zero);

            // Assert
            Assert.NotNull(entry.Response.Headers);
            Assert.True(entry.Response.Headers.Count > 0);
        }

        [Fact]
        public void PopulateResponse_LogResponseHeadersDisabled_NoHeaders()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Response.Headers["X-Request-Id"] = "abc123";
            var config = CreateConfig(logResponseHeaders: false);
            var entry = _factory.CreateEntry(httpContext, config);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, TimeSpan.Zero);

            // Assert
            Assert.Null(entry.Response.Headers);
        }

        [Fact]
        public void PopulateResponse_SetCookieHeader_IsMasked()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.Response.Headers["Set-Cookie"] = "session=abc123; Path=/";
            var config = CreateConfig(logResponseHeaders: true);
            var entry = _factory.CreateEntry(httpContext, config);

            // Act
            _factory.PopulateResponse(entry, httpContext, config, null, TimeSpan.Zero);

            // Assert
            Assert.NotNull(entry.Response.Headers);
            Assert.Equal("***", entry.Response.Headers["Set-Cookie"]);
        }

        #endregion

        #region Helper Methods

        private static HttpContext CreateHttpContext(
            string method = "GET",
            string path = "/api/test",
            string? queryString = null,
            string? contentType = null,
            long? contentLength = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;
            httpContext.Request.Path = path;

            if (!string.IsNullOrEmpty(queryString))
            {
                httpContext.Request.QueryString = new QueryString(queryString);
            }

            if (!string.IsNullOrEmpty(contentType))
            {
                httpContext.Request.ContentType = contentType;
            }

            if (contentLength.HasValue)
            {
                httpContext.Request.ContentLength = contentLength.Value;
            }

            return httpContext;
        }

        private static ApiLoggingEffectiveConfig CreateConfig(
            bool logRequestHeaders = false,
            bool logResponseHeaders = false)
        {
            return new ApiLoggingEffectiveConfig
            {
                Enabled = true,
                LogRequestHeaders = logRequestHeaders,
                LogResponseHeaders = logResponseHeaders,
                LogRequestBody = true,
                LogResponseBody = true
            };
        }

        #endregion
    }
}
