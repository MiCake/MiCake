using MiCake.AspNetCore.ApiLogging;
using System.Collections.Generic;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="ApiLoggingOptions"/> configuration class.
    /// </summary>
    public class ApiLoggingOptions_Tests
    {
        #region Default Value Tests

        [Fact]
        public void ApiLoggingOptions_DefaultEnabled_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.True(options.Enabled);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultExcludeStatusCodes_ShouldBeEmpty()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.NotNull(options.ExcludeStatusCodes);
            Assert.Empty(options.ExcludeStatusCodes);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultMaxRequestBodySize_ShouldBe4KB()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.Equal(4096, options.MaxRequestBodySize);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultMaxResponseBodySize_ShouldBe4KB()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.Equal(4096, options.MaxResponseBodySize);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultTruncationStrategy_ShouldBeTruncateWithSummary()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.Equal(TruncationStrategy.TruncateWithSummary, options.TruncationStrategy);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultExcludedPaths_ShouldContainHealthAndMetrics()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.NotNull(options.ExcludedPaths);
            Assert.Contains("/health", options.ExcludedPaths);
            Assert.Contains("/metrics", options.ExcludedPaths);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultExcludedContentTypes_ShouldContainBinaryTypes()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.NotNull(options.ExcludedContentTypes);
            Assert.Contains("application/octet-stream", options.ExcludedContentTypes);
            Assert.Contains("image/*", options.ExcludedContentTypes);
            Assert.Contains("video/*", options.ExcludedContentTypes);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultSensitiveFields_ShouldContainCommonSensitiveNames()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.NotNull(options.SensitiveFields);
            Assert.Contains("authorization", options.SensitiveFields);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultLogRequestHeaders_ShouldBeFalse()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.False(options.LogRequestHeaders);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultLogResponseHeaders_ShouldBeFalse()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.False(options.LogResponseHeaders);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultLogRequestBody_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.True(options.LogRequestBody);
        }

        [Fact]
        public void ApiLoggingOptions_DefaultLogResponseBody_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new ApiLoggingOptions();

            // Assert
            Assert.True(options.LogResponseBody);
        }

        #endregion

        #region Property Assignment Tests

        [Fact]
        public void ApiLoggingOptions_CanSetEnabled()
        {
            // Arrange
            var options = new ApiLoggingOptions();

            // Act
            options.Enabled = false;

            // Assert
            Assert.False(options.Enabled);
        }

        [Theory]
        [InlineData(200)]
        [InlineData(204)]
        [InlineData(400)]
        [InlineData(500)]
        public void ApiLoggingOptions_CanAddExcludeStatusCodes(int statusCode)
        {
            // Arrange
            var options = new ApiLoggingOptions();

            // Act
            options.ExcludeStatusCodes.Add(statusCode);

            // Assert
            Assert.Contains(statusCode, options.ExcludeStatusCodes);
        }

        [Fact]
        public void ApiLoggingOptions_CanReplaceExcludeStatusCodes()
        {
            // Arrange
            var options = new ApiLoggingOptions();
            var newCodes = new List<int> { 200, 201, 204 };

            // Act
            options.ExcludeStatusCodes = newCodes;

            // Assert
            Assert.Equal(3, options.ExcludeStatusCodes.Count);
            Assert.Contains(200, options.ExcludeStatusCodes);
            Assert.Contains(201, options.ExcludeStatusCodes);
            Assert.Contains(204, options.ExcludeStatusCodes);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(8192)]
        [InlineData(16384)]
        public void ApiLoggingOptions_CanSetMaxRequestBodySize(int size)
        {
            // Arrange
            var options = new ApiLoggingOptions();

            // Act
            options.MaxRequestBodySize = size;

            // Assert
            Assert.Equal(size, options.MaxRequestBodySize);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(8192)]
        [InlineData(16384)]
        public void ApiLoggingOptions_CanSetMaxResponseBodySize(int size)
        {
            // Arrange
            var options = new ApiLoggingOptions();

            // Act
            options.MaxResponseBodySize = size;

            // Assert
            Assert.Equal(size, options.MaxResponseBodySize);
        }

        [Theory]
        [InlineData(TruncationStrategy.SimpleTruncate)]
        [InlineData(TruncationStrategy.TruncateWithSummary)]
        [InlineData(TruncationStrategy.MetadataOnly)]
        public void ApiLoggingOptions_CanSetTruncationStrategy(TruncationStrategy strategy)
        {
            // Arrange
            var options = new ApiLoggingOptions();

            // Act
            options.TruncationStrategy = strategy;

            // Assert
            Assert.Equal(strategy, options.TruncationStrategy);
        }

        [Fact]
        public void ApiLoggingOptions_CanAddCustomSensitiveField()
        {
            // Arrange
            var options = new ApiLoggingOptions();

            // Act
            options.SensitiveFields.Add("creditCard");
            options.SensitiveFields.Add("ssn");

            // Assert
            Assert.Contains("creditCard", options.SensitiveFields);
            Assert.Contains("ssn", options.SensitiveFields);
        }

        [Fact]
        public void ApiLoggingOptions_CanAddCustomExcludedPath()
        {
            // Arrange
            var options = new ApiLoggingOptions();

            // Act
            options.ExcludedPaths.Add("/api/files/*");
            options.ExcludedPaths.Add("/api/export/**");

            // Assert
            Assert.Contains("/api/files/*", options.ExcludedPaths);
            Assert.Contains("/api/export/**", options.ExcludedPaths);
        }

        #endregion

        #region TruncationStrategy Enum Tests

        [Fact]
        public void TruncationStrategy_SimpleTruncate_HasCorrectValue()
        {
            // Assert
            Assert.Equal(0, (int)TruncationStrategy.SimpleTruncate);
        }

        [Fact]
        public void TruncationStrategy_TruncateWithSummary_HasCorrectValue()
        {
            // Assert
            Assert.Equal(1, (int)TruncationStrategy.TruncateWithSummary);
        }

        [Fact]
        public void TruncationStrategy_MetadataOnly_HasCorrectValue()
        {
            // Assert
            Assert.Equal(2, (int)TruncationStrategy.MetadataOnly);
        }

        #endregion
    }
}
