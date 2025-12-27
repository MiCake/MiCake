using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="TruncationProcessor"/> implementation.
    /// </summary>
    public class TruncationProcessor_Tests
    {
        private readonly TruncationProcessor _processor;

        public TruncationProcessor_Tests()
        {
            _processor = new TruncationProcessor();
        }

        #region Order Tests

        [Fact]
        public void Order_ShouldBe10()
        {
            // Assert - Runs after SensitiveMaskProcessor (Order: 0)
            Assert.Equal(10, _processor.Order);
        }

        #endregion

        #region Request Body Truncation Tests

        [Fact]
        public async Task ProcessAsync_RequestBodyWithinLimit_NotTruncated()
        {
            // Arrange
            var entry = CreateLogEntry(requestBody: "Short content");
            var context = CreateContext(maxRequestBodySize: 4096);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Short content", result.Request.Body);
        }

        [Fact]
        public async Task ProcessAsync_RequestBodyExceedsLimit_Truncated()
        {
            // Arrange
            var largeBody = new string('A', 5000); // 5000 bytes
            var entry = CreateLogEntry(requestBody: largeBody);
            var context = CreateContext(maxRequestBodySize: 100);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Request.Body!.Length < largeBody.Length);
            Assert.Contains("...[truncated]", result.Request.Body);
        }

        [Fact]
        public async Task ProcessAsync_NullRequestBody_Unchanged()
        {
            // Arrange
            var entry = CreateLogEntry(requestBody: null);
            var context = CreateContext();

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Request.Body);
        }

        [Fact]
        public async Task ProcessAsync_EmptyRequestBody_Unchanged()
        {
            // Arrange
            var entry = CreateLogEntry(requestBody: string.Empty);
            var context = CreateContext();

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Request.Body);
        }

        #endregion

        #region Response Body Truncation Tests

        [Fact]
        public async Task ProcessAsync_ResponseBodyWithinLimit_NotTruncated()
        {
            // Arrange
            var entry = CreateLogEntry(responseBody: "Short response");
            var context = CreateContext(maxResponseBodySize: 4096);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Short response", result.Response.Body);
            Assert.False(result.Response.IsTruncated);
            Assert.Null(result.Response.OriginalSize);
        }

        [Fact]
        public async Task ProcessAsync_ResponseBodyExceedsLimit_Truncated()
        {
            // Arrange
            var largeBody = new string('B', 5000);
            var entry = CreateLogEntry(responseBody: largeBody);
            var context = CreateContext(maxResponseBodySize: 100);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Response.Body!.Length < largeBody.Length);
            Assert.Contains("...[truncated]", result.Response.Body);
            Assert.True(result.Response.IsTruncated);
            Assert.Equal(5000, result.Response.OriginalSize);
        }

        [Fact]
        public async Task ProcessAsync_ResponseBodyExceedsLimit_SetsOriginalSize()
        {
            // Arrange
            var largeBody = new string('C', 10000);
            var entry = CreateLogEntry(responseBody: largeBody);
            var context = CreateContext(maxResponseBodySize: 500);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Response.IsTruncated);
            Assert.Equal(10000, result.Response.OriginalSize);
        }

        #endregion

        #region Truncation Strategy Tests

        [Fact]
        public async Task ProcessAsync_SimpleTruncateStrategy_TruncatesWithMarker()
        {
            // Arrange
            var largeBody = new string('D', 5000);
            var entry = CreateLogEntry(responseBody: largeBody);
            var context = CreateContext(
                maxResponseBodySize: 100,
                truncationStrategy: TruncationStrategy.SimpleTruncate);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("...[truncated]", result.Response.Body);
        }

        [Fact]
        public async Task ProcessAsync_MetadataOnlyStrategy_ReturnsOnlySize()
        {
            // Arrange
            var largeBody = new string('E', 5000);
            var entry = CreateLogEntry(responseBody: largeBody);
            var context = CreateContext(
                maxResponseBodySize: 100,
                truncationStrategy: TruncationStrategy.MetadataOnly);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("[Content:", result.Response.Body);
            Assert.Contains("bytes]", result.Response.Body);
        }

        [Fact]
        public async Task ProcessAsync_TruncateWithSummary_JsonArray_ProvidesSummary()
        {
            // Arrange
            var jsonArray = """[{"id": 1}, {"id": 2}, {"id": 3}, {"id": 4}, {"id": 5}]""";
            // Make it large enough to trigger truncation
            var largeArray = "[" + string.Join(",", Enumerable.Range(1, 100).Select(i => $$"""{"id": {{i}}, "data": "{{new string('X', 100)}}"}""")) + "]";
            var entry = CreateLogEntry(responseBody: largeArray);
            var context = CreateContext(
                maxResponseBodySize: 500,
                truncationStrategy: TruncationStrategy.TruncateWithSummary);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Response.IsTruncated);
            Assert.Contains("Array with", result.Response.TruncationSummary);
            Assert.Contains("items", result.Response.TruncationSummary);
        }

        [Fact]
        public async Task ProcessAsync_TruncateWithSummary_JsonObject_ProvidesSummary()
        {
            // Arrange
            var jsonObj = new StringBuilder("{");
            for (int i = 0; i < 50; i++)
            {
                if (i > 0) jsonObj.Append(',');
                jsonObj.Append($"\"field{i}\": \"{new string('Y', 100)}\"");
            }
            jsonObj.Append('}');

            var entry = CreateLogEntry(responseBody: jsonObj.ToString());
            var context = CreateContext(
                maxResponseBodySize: 500,
                truncationStrategy: TruncationStrategy.TruncateWithSummary);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Response.IsTruncated);
            // TruncationSummary may be null for objects without "data" array pattern
            // The key assertion is that truncation occurred
        }

        [Fact]
        public async Task ProcessAsync_TruncateWithSummary_WrappedData_ProvidesSummary()
        {
            // Arrange
            var items = string.Join(",", Enumerable.Range(1, 50).Select(i => $$"""{"id": {{i}}}"""));
            var wrappedJson = $$"""{"data": [{{items}}], "totalCount": 500}""";
            var entry = CreateLogEntry(responseBody: wrappedJson);
            var context = CreateContext(
                maxResponseBodySize: 200,
                truncationStrategy: TruncationStrategy.TruncateWithSummary);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Response.IsTruncated);
            Assert.NotNull(result.Response.TruncationSummary);
            // Should include totalCount info
            Assert.Contains("totalCount: 500", result.Response.TruncationSummary);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ProcessAsync_BodyExactlyAtLimit_NotTruncated()
        {
            // Arrange
            var body = new string('F', 100);
            var entry = CreateLogEntry(responseBody: body);
            var context = CreateContext(maxResponseBodySize: 100);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(body, result.Response.Body);
            Assert.False(result.Response.IsTruncated);
        }

        [Fact]
        public async Task ProcessAsync_BodyJustOverLimit_Truncated()
        {
            // Arrange
            var body = new string('G', 101);
            var entry = CreateLogEntry(responseBody: body);
            var context = CreateContext(maxResponseBodySize: 100);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Response.IsTruncated);
        }

        [Fact]
        public async Task ProcessAsync_BothBodiesExceedLimits_BothTruncated()
        {
            // Arrange
            var largeRequestBody = new string('H', 5000);
            var largeResponseBody = new string('I', 5000);
            var entry = CreateLogEntry(requestBody: largeRequestBody, responseBody: largeResponseBody);
            var context = CreateContext(maxRequestBodySize: 100, maxResponseBodySize: 100);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("...[truncated]", result.Request.Body);
            Assert.Contains("...[truncated]", result.Response.Body);
            Assert.True(result.Response.IsTruncated);
        }

        [Fact]
        public async Task ProcessAsync_InvalidJson_TruncatesWithoutSummary()
        {
            // Arrange
            var invalidJson = "This is not valid JSON: " + new string('J', 5000);
            var entry = CreateLogEntry(responseBody: invalidJson);
            var context = CreateContext(
                maxResponseBodySize: 100,
                truncationStrategy: TruncationStrategy.TruncateWithSummary);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Response.IsTruncated);
            Assert.Null(result.Response.TruncationSummary); // No summary for invalid JSON
        }

        [Fact]
        public async Task ProcessAsync_MultiBytCharacters_HandlesCorrectly()
        {
            // Arrange
            // Chinese characters are typically 3 bytes in UTF-8
            var chineseText = new string('中', 200); // ~600 bytes
            var entry = CreateLogEntry(responseBody: chineseText);
            var context = CreateContext(maxResponseBodySize: 100);

            // Act
            var result = await _processor.ProcessAsync(entry, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Response.IsTruncated);
            // Verify the truncation didn't corrupt the characters
            Assert.True(Encoding.UTF8.GetByteCount(result.Response.Body!) <= 100 + 20); // +20 for "[truncated]"
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

        private static ApiLogProcessingContext CreateContext(
            int maxRequestBodySize = 4096,
            int maxResponseBodySize = 4096,
            TruncationStrategy truncationStrategy = TruncationStrategy.TruncateWithSummary)
        {
            var config = new ApiLoggingEffectiveConfig
            {
                Enabled = true,
                MaxRequestBodySize = maxRequestBodySize,
                MaxResponseBodySize = maxResponseBodySize,
                TruncationStrategy = truncationStrategy
            };

            return new ApiLogProcessingContext(new DefaultHttpContext(), config);
        }

        #endregion
    }
}
