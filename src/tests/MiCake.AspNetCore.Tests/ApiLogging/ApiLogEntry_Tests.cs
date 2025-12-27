using MiCake.AspNetCore.ApiLogging;
using System;
using System.Collections.Generic;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="ApiLogEntry"/>, <see cref="ApiRequestLog"/>, 
    /// and <see cref="ApiResponseLog"/> model classes.
    /// </summary>
    public class ApiLogEntry_Tests
    {
        #region ApiLogEntry Tests

        [Fact]
        public void ApiLogEntry_DefaultCorrelationId_IsEmpty()
        {
            // Arrange & Act
            var entry = new ApiLogEntry();

            // Assert
            Assert.Equal(string.Empty, entry.CorrelationId);
        }

        [Fact]
        public void ApiLogEntry_CanSetCorrelationId()
        {
            // Arrange
            var entry = new ApiLogEntry();

            // Act
            entry.CorrelationId = "test-correlation-123";

            // Assert
            Assert.Equal("test-correlation-123", entry.CorrelationId);
        }

        [Fact]
        public void ApiLogEntry_DefaultTimestamp_IsDefault()
        {
            // Arrange & Act
            var entry = new ApiLogEntry();

            // Assert
            Assert.Equal(default, entry.Timestamp);
        }

        [Fact]
        public void ApiLogEntry_CanSetTimestamp()
        {
            // Arrange
            var entry = new ApiLogEntry();
            var now = DateTimeOffset.UtcNow;

            // Act
            entry.Timestamp = now;

            // Assert
            Assert.Equal(now, entry.Timestamp);
        }

        [Fact]
        public void ApiLogEntry_DefaultRequest_IsNotNull()
        {
            // Arrange & Act
            var entry = new ApiLogEntry();

            // Assert
            Assert.NotNull(entry.Request);
        }

        [Fact]
        public void ApiLogEntry_DefaultResponse_IsNotNull()
        {
            // Arrange & Act
            var entry = new ApiLogEntry();

            // Assert
            Assert.NotNull(entry.Response);
        }

        [Fact]
        public void ApiLogEntry_DefaultElapsedMilliseconds_IsZero()
        {
            // Arrange & Act
            var entry = new ApiLogEntry();

            // Assert
            Assert.Equal(0, entry.ElapsedMilliseconds);
        }

        [Fact]
        public void ApiLogEntry_CanSetElapsedMilliseconds()
        {
            // Arrange
            var entry = new ApiLogEntry();

            // Act
            entry.ElapsedMilliseconds = 150;

            // Assert
            Assert.Equal(150, entry.ElapsedMilliseconds);
        }

        #endregion

        #region ApiRequestLog Tests

        [Fact]
        public void ApiRequestLog_DefaultMethod_IsEmpty()
        {
            // Arrange & Act
            var request = new ApiRequestLog();

            // Assert
            Assert.Equal(string.Empty, request.Method);
        }

        [Fact]
        public void ApiRequestLog_CanSetMethod()
        {
            // Arrange
            var request = new ApiRequestLog();

            // Act
            request.Method = "POST";

            // Assert
            Assert.Equal("POST", request.Method);
        }

        [Fact]
        public void ApiRequestLog_DefaultPath_IsEmpty()
        {
            // Arrange & Act
            var request = new ApiRequestLog();

            // Assert
            Assert.Equal(string.Empty, request.Path);
        }

        [Fact]
        public void ApiRequestLog_CanSetPath()
        {
            // Arrange
            var request = new ApiRequestLog();

            // Act
            request.Path = "/api/users";

            // Assert
            Assert.Equal("/api/users", request.Path);
        }

        [Fact]
        public void ApiRequestLog_DefaultQueryString_IsNull()
        {
            // Arrange & Act
            var request = new ApiRequestLog();

            // Assert
            Assert.Null(request.QueryString);
        }

        [Fact]
        public void ApiRequestLog_CanSetQueryString()
        {
            // Arrange
            var request = new ApiRequestLog();

            // Act
            request.QueryString = "?page=1&size=10";

            // Assert
            Assert.Equal("?page=1&size=10", request.QueryString);
        }

        [Fact]
        public void ApiRequestLog_DefaultBody_IsNull()
        {
            // Arrange & Act
            var request = new ApiRequestLog();

            // Assert
            Assert.Null(request.Body);
        }

        [Fact]
        public void ApiRequestLog_CanSetBody()
        {
            // Arrange
            var request = new ApiRequestLog();

            // Act
            request.Body = """{"name": "test"}""";

            // Assert
            Assert.Equal("""{"name": "test"}""", request.Body);
        }

        [Fact]
        public void ApiRequestLog_DefaultHeaders_IsNull()
        {
            // Arrange & Act
            var request = new ApiRequestLog();

            // Assert
            Assert.Null(request.Headers);
        }

        [Fact]
        public void ApiRequestLog_CanSetHeaders()
        {
            // Arrange
            var request = new ApiRequestLog();
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Accept"] = "application/json"
            };

            // Act
            request.Headers = headers;

            // Assert
            Assert.NotNull(request.Headers);
            Assert.Equal(2, request.Headers.Count);
        }

        [Fact]
        public void ApiRequestLog_DefaultContentType_IsNull()
        {
            // Arrange & Act
            var request = new ApiRequestLog();

            // Assert
            Assert.Null(request.ContentType);
        }

        [Fact]
        public void ApiRequestLog_DefaultContentLength_IsNull()
        {
            // Arrange & Act
            var request = new ApiRequestLog();

            // Assert
            Assert.Null(request.ContentLength);
        }

        #endregion

        #region ApiResponseLog Tests

        [Fact]
        public void ApiResponseLog_DefaultStatusCode_IsZero()
        {
            // Arrange & Act
            var response = new ApiResponseLog();

            // Assert
            Assert.Equal(0, response.StatusCode);
        }

        [Fact]
        public void ApiResponseLog_CanSetStatusCode()
        {
            // Arrange
            var response = new ApiResponseLog();

            // Act
            response.StatusCode = 200;

            // Assert
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public void ApiResponseLog_DefaultBody_IsNull()
        {
            // Arrange & Act
            var response = new ApiResponseLog();

            // Assert
            Assert.Null(response.Body);
        }

        [Fact]
        public void ApiResponseLog_CanSetBody()
        {
            // Arrange
            var response = new ApiResponseLog();

            // Act
            response.Body = """{"id": 123}""";

            // Assert
            Assert.Equal("""{"id": 123}""", response.Body);
        }

        [Fact]
        public void ApiResponseLog_DefaultHeaders_IsNull()
        {
            // Arrange & Act
            var response = new ApiResponseLog();

            // Assert
            Assert.Null(response.Headers);
        }

        [Fact]
        public void ApiResponseLog_DefaultContentType_IsNull()
        {
            // Arrange & Act
            var response = new ApiResponseLog();

            // Assert
            Assert.Null(response.ContentType);
        }

        [Fact]
        public void ApiResponseLog_DefaultContentLength_IsNull()
        {
            // Arrange & Act
            var response = new ApiResponseLog();

            // Assert
            Assert.Null(response.ContentLength);
        }

        [Fact]
        public void ApiResponseLog_DefaultIsTruncated_IsFalse()
        {
            // Arrange & Act
            var response = new ApiResponseLog();

            // Assert
            Assert.False(response.IsTruncated);
        }

        [Fact]
        public void ApiResponseLog_CanSetIsTruncated()
        {
            // Arrange
            var response = new ApiResponseLog();

            // Act
            response.IsTruncated = true;

            // Assert
            Assert.True(response.IsTruncated);
        }

        [Fact]
        public void ApiResponseLog_DefaultOriginalSize_IsNull()
        {
            // Arrange & Act
            var response = new ApiResponseLog();

            // Assert
            Assert.Null(response.OriginalSize);
        }

        [Fact]
        public void ApiResponseLog_CanSetOriginalSize()
        {
            // Arrange
            var response = new ApiResponseLog();

            // Act
            response.OriginalSize = 10000;

            // Assert
            Assert.Equal(10000, response.OriginalSize);
        }

        [Fact]
        public void ApiResponseLog_DefaultTruncationSummary_IsNull()
        {
            // Arrange & Act
            var response = new ApiResponseLog();

            // Assert
            Assert.Null(response.TruncationSummary);
        }

        [Fact]
        public void ApiResponseLog_CanSetTruncationSummary()
        {
            // Arrange
            var response = new ApiResponseLog();

            // Act
            response.TruncationSummary = "Array with 500 items";

            // Assert
            Assert.Equal("Array with 500 items", response.TruncationSummary);
        }

        #endregion

        #region Complete Entry Tests

        [Fact]
        public void ApiLogEntry_CanCreateCompleteEntry()
        {
            // Arrange & Act
            var entry = new ApiLogEntry
            {
                CorrelationId = "abc-123",
                Timestamp = DateTimeOffset.UtcNow,
                ElapsedMilliseconds = 150,
                Request = new ApiRequestLog
                {
                    Method = "POST",
                    Path = "/api/users",
                    QueryString = "?validate=true",
                    Body = """{"name": "test"}""",
                    ContentType = "application/json",
                    ContentLength = 16,
                    Headers = new Dictionary<string, string>
                    {
                        ["Accept"] = "application/json"
                    }
                },
                Response = new ApiResponseLog
                {
                    StatusCode = 201,
                    Body = """{"id": 1, "name": "test"}""",
                    ContentType = "application/json",
                    ContentLength = 25,
                    IsTruncated = false
                }
            };

            // Assert
            Assert.Equal("abc-123", entry.CorrelationId);
            Assert.Equal(150, entry.ElapsedMilliseconds);
            Assert.Equal("POST", entry.Request.Method);
            Assert.Equal("/api/users", entry.Request.Path);
            Assert.Equal(201, entry.Response.StatusCode);
        }

        [Fact]
        public void ApiLogEntry_CanCreateTruncatedEntry()
        {
            // Arrange & Act
            var entry = new ApiLogEntry
            {
                Response = new ApiResponseLog
                {
                    StatusCode = 200,
                    Body = """{"data": [...truncated...]}""",
                    IsTruncated = true,
                    OriginalSize = 50000,
                    TruncationSummary = "Array with 1000 items"
                }
            };

            // Assert
            Assert.True(entry.Response.IsTruncated);
            Assert.Equal(50000, entry.Response.OriginalSize);
            Assert.Equal("Array with 1000 items", entry.Response.TruncationSummary);
        }

        #endregion
    }
}
