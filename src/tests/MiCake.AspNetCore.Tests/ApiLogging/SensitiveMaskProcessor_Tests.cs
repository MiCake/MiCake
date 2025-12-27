using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="SensitiveMaskProcessor"/> implementation.
    /// </summary>
    public class SensitiveMaskProcessor_Tests
    {
        private readonly SensitiveMaskProcessor _processor;

        public SensitiveMaskProcessor_Tests()
        {
            _processor = new SensitiveMaskProcessor(new JsonSensitiveDataMasker());
        }

        #region Order Tests

        [Fact]
        public void Order_ShouldBe0()
        {
            // Assert - Should run first
            Assert.Equal(0, _processor.Order);
        }

        #endregion

        #region No Sensitive Fields Tests

        [Fact]
        public async Task ProcessAsync_NoSensitiveFields_ReturnsUnchanged()
        {
            // Arrange
            var entry = CreateLogEntry(
                requestBody: """{"password": "secret123"}""",
                responseBody: """{"token": "abc123"}""");
            var context = CreateContext(sensitiveFields: []);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("secret123", result.Request.Body);
            Assert.Contains("abc123", result.Response.Body);
        }

        #endregion

        #region Request Body Masking Tests

        [Fact]
        public async Task ProcessAsync_RequestBodyWithSensitiveField_Masked()
        {
            // Arrange
            var entry = CreateLogEntry(
                requestBody: """{"username": "john", "password": "secret123"}""");
            var context = CreateContext(sensitiveFields: ["password"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("secret123", result.Request.Body);
            Assert.Contains("john", result.Request.Body);
        }

        [Fact]
        public async Task ProcessAsync_NullRequestBody_HandlesGracefully()
        {
            // Arrange
            var entry = CreateLogEntry(requestBody: null);
            var context = CreateContext(sensitiveFields: ["password"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Request.Body);
        }

        [Fact]
        public async Task ProcessAsync_EmptyRequestBody_HandlesGracefully()
        {
            // Arrange
            var entry = CreateLogEntry(requestBody: string.Empty);
            var context = CreateContext(sensitiveFields: ["password"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Request.Body);
        }

        #endregion

        #region Response Body Masking Tests

        [Fact]
        public async Task ProcessAsync_ResponseBodyWithSensitiveField_Masked()
        {
            // Arrange
            var entry = CreateLogEntry(
                responseBody: """{"user": "john", "token": "xyz789"}""");
            var context = CreateContext(sensitiveFields: ["token"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("xyz789", result.Response.Body);
            Assert.Contains("john", result.Response.Body);
        }

        [Fact]
        public async Task ProcessAsync_NullResponseBody_HandlesGracefully()
        {
            // Arrange
            var entry = CreateLogEntry(responseBody: null);
            var context = CreateContext(sensitiveFields: ["token"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Response.Body);
        }

        #endregion

        #region Query String Masking Tests

        [Fact]
        public async Task ProcessAsync_QueryStringWithSensitiveField_Masked()
        {
            // Arrange
            var entry = CreateLogEntry();
            entry.Request.QueryString = "?userId=123&token=secret456";
            var context = CreateContext(sensitiveFields: ["token"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("secret456", result.Request.QueryString);
            Assert.Contains("userId=123", result.Request.QueryString);
        }

        [Fact]
        public async Task ProcessAsync_NullQueryString_HandlesGracefully()
        {
            // Arrange
            var entry = CreateLogEntry();
            entry.Request.QueryString = null;
            var context = CreateContext(sensitiveFields: ["token"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Request.QueryString);
        }

        #endregion

        #region Multiple Sensitive Fields Tests

        [Fact]
        public async Task ProcessAsync_MultipleSensitiveFields_AllMasked()
        {
            // Arrange
            var entry = CreateLogEntry(
                requestBody: """{"password": "pass123", "secret": "sec456", "key": "key789"}""");
            var context = CreateContext(sensitiveFields: ["password", "secret", "key"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("pass123", result.Request.Body);
            Assert.DoesNotContain("sec456", result.Request.Body);
            Assert.DoesNotContain("key789", result.Request.Body);
        }

        #endregion

        #region Both Bodies Masking Tests

        [Fact]
        public async Task ProcessAsync_BothBodiesWithSensitiveFields_BothMasked()
        {
            // Arrange
            var entry = CreateLogEntry(
                requestBody: """{"password": "reqSecret"}""",
                responseBody: """{"password": "respSecret"}""");
            var context = CreateContext(sensitiveFields: ["password"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("reqSecret", result.Request.Body);
            Assert.DoesNotContain("respSecret", result.Response.Body);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ProcessAsync_NonJsonContent_StillAttemptsMasking()
        {
            // Arrange
            var entry = CreateLogEntry(
                requestBody: "username=john&password=secret123");
            var context = CreateContext(sensitiveFields: ["password"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            // Regex fallback should mask the password
            Assert.DoesNotContain("secret123", result.Request.Body);
        }

        [Fact]
        public async Task ProcessAsync_ReturnsEntry_NotNull()
        {
            // Arrange
            var entry = CreateLogEntry();
            var context = CreateContext(sensitiveFields: ["password"]);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Same(entry, result); // Should return the same entry (modified)
        }

        #endregion

        #region Helper Methods

        private static ApiLogEntry CreateLogEntry(
            string? requestBody = null,
            string? responseBody = null)
        {
            return new ApiLogEntry
            {
                CorrelationId = "test-correlation-id",
                Request = new ApiRequestLog
                {
                    Method = "POST",
                    Path = "/api/test",
                    Body = requestBody
                },
                Response = new ApiResponseLog
                {
                    StatusCode = 200,
                    Body = responseBody
                }
            };
        }

        private static ApiLogProcessingContext CreateContext(List<string> sensitiveFields)
        {
            var config = new ApiLoggingEffectiveConfig
            {
                Enabled = true,
                SensitiveFields = sensitiveFields
            };

            return new ApiLogProcessingContext(new DefaultHttpContext(), config);
        }

        #endregion
    }
}
