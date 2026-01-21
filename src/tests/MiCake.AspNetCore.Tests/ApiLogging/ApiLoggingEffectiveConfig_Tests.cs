using MiCake.AspNetCore.ApiLogging;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="ApiLoggingEffectiveConfig"/> class.
    /// </summary>
    public class ApiLoggingEffectiveConfig_Tests
    {
        #region Default Value Tests

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultEnabled_IsTrue()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.True(config.Enabled);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultExcludeStatusCodes_IsEmpty()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.NotNull(config.ExcludeStatusCodes);
            Assert.Empty(config.ExcludeStatusCodes);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultExcludedPaths_IsEmpty()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.NotNull(config.ExcludedPaths);
            Assert.Empty(config.ExcludedPaths);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultSensitiveFields_IsEmpty()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.NotNull(config.SensitiveFields);
            Assert.Empty(config.SensitiveFields);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultMaxRequestBodySize_Is4096()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.Equal(4096, config.MaxRequestBodySize);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultMaxResponseBodySize_Is4096()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.Equal(4096, config.MaxResponseBodySize);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultTruncationStrategy_IsTruncateWithSummary()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.Equal(TruncationStrategy.TruncateWithSummary, config.TruncationStrategy);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultLogRequestHeaders_IsFalse()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.False(config.LogRequestHeaders);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultLogResponseHeaders_IsFalse()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.False(config.LogResponseHeaders);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultLogRequestBody_IsTrue()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.True(config.LogRequestBody);
        }

        [Fact]
        public void ApiLoggingEffectiveConfig_DefaultLogResponseBody_IsTrue()
        {
            // Arrange & Act
            var config = new ApiLoggingEffectiveConfig();

            // Assert
            Assert.True(config.LogResponseBody);
        }

        #endregion

        #region FromOptions Tests

        [Fact]
        public void FromOptions_CopiesEnabled()
        {
            // Arrange
            var options = new ApiLoggingOptions { Enabled = false };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.False(config.Enabled);
        }

        [Fact]
        public void FromOptions_CopiesExcludeStatusCodes()
        {
            // Arrange
            var options = new ApiLoggingOptions
            {
                ExcludeStatusCodes = [200, 204, 304]
            };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.Equal(3, config.ExcludeStatusCodes.Count);
            Assert.Contains(200, config.ExcludeStatusCodes);
            Assert.Contains(204, config.ExcludeStatusCodes);
            Assert.Contains(304, config.ExcludeStatusCodes);
        }

        [Fact]
        public void FromOptions_CreatesNewListForExcludeStatusCodes()
        {
            // Arrange
            var options = new ApiLoggingOptions
            {
                ExcludeStatusCodes = [200]
            };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);
            options.ExcludeStatusCodes.Add(500);

            // Assert - Modification to original doesn't affect config
            Assert.Single(config.ExcludeStatusCodes);
        }

        [Fact]
        public void FromOptions_CopiesExcludedPaths()
        {
            // Arrange
            var options = new ApiLoggingOptions
            {
                ExcludedPaths = ["/health", "/metrics", "/swagger/*"]
            };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.Equal(3, config.ExcludedPaths.Count);
            Assert.Contains("/health", config.ExcludedPaths);
            Assert.Contains("/swagger/*", config.ExcludedPaths);
        }

        [Fact]
        public void FromOptions_CopiesExcludedContentTypes()
        {
            // Arrange
            var options = new ApiLoggingOptions
            {
                ExcludedContentTypes = ["application/octet-stream", "image/*"]
            };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.Equal(2, config.ExcludedContentTypes.Count);
            Assert.Contains("application/octet-stream", config.ExcludedContentTypes);
            Assert.Contains("image/*", config.ExcludedContentTypes);
        }

        [Fact]
        public void FromOptions_CopiesSensitiveFields()
        {
            // Arrange
            var options = new ApiLoggingOptions
            {
                SensitiveFields = ["password", "token", "secret"]
            };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.Equal(3, config.SensitiveFields.Count);
            Assert.Contains("password", config.SensitiveFields);
            Assert.Contains("token", config.SensitiveFields);
            Assert.Contains("secret", config.SensitiveFields);
        }

        [Fact]
        public void FromOptions_CopiesMaxRequestBodySize()
        {
            // Arrange
            var options = new ApiLoggingOptions { MaxRequestBodySize = 8192 };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.Equal(8192, config.MaxRequestBodySize);
        }

        [Fact]
        public void FromOptions_CopiesMaxResponseBodySize()
        {
            // Arrange
            var options = new ApiLoggingOptions { MaxResponseBodySize = 16384 };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.Equal(16384, config.MaxResponseBodySize);
        }

        [Fact]
        public void FromOptions_CopiesTruncationStrategy()
        {
            // Arrange
            var options = new ApiLoggingOptions
            {
                TruncationStrategy = TruncationStrategy.MetadataOnly
            };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.Equal(TruncationStrategy.MetadataOnly, config.TruncationStrategy);
        }

        [Fact]
        public void FromOptions_CopiesLogRequestHeaders()
        {
            // Arrange
            var options = new ApiLoggingOptions { LogRequestHeaders = true };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.True(config.LogRequestHeaders);
        }

        [Fact]
        public void FromOptions_CopiesLogResponseHeaders()
        {
            // Arrange
            var options = new ApiLoggingOptions { LogResponseHeaders = true };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.True(config.LogResponseHeaders);
        }

        [Fact]
        public void FromOptions_CopiesLogRequestBody()
        {
            // Arrange
            var options = new ApiLoggingOptions { LogRequestBody = false };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.False(config.LogRequestBody);
        }

        [Fact]
        public void FromOptions_CopiesLogResponseBody()
        {
            // Arrange
            var options = new ApiLoggingOptions { LogResponseBody = false };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.False(config.LogResponseBody);
        }

        [Fact]
        public void FromOptions_CopiesAllSettings()
        {
            // Arrange
            var options = new ApiLoggingOptions
            {
                Enabled = true,
                ExcludeStatusCodes = [200, 204],
                ExcludedPaths = ["/health"],
                ExcludedContentTypes = ["image/*"],
                SensitiveFields = ["password"],
                MaxRequestBodySize = 8192,
                MaxResponseBodySize = 16384,
                TruncationStrategy = TruncationStrategy.SimpleTruncate,
                LogRequestHeaders = true,
                LogResponseHeaders = true,
                LogRequestBody = true,
                LogResponseBody = true
            };

            // Act
            var config = ApiLoggingEffectiveConfig.FromOptions(options);

            // Assert
            Assert.True(config.Enabled);
            Assert.Equal(2, config.ExcludeStatusCodes.Count);
            Assert.Single(config.ExcludedPaths);
            Assert.Single(config.ExcludedContentTypes);
            Assert.Single(config.SensitiveFields);
            Assert.Equal(8192, config.MaxRequestBodySize);
            Assert.Equal(16384, config.MaxResponseBodySize);
            Assert.Equal(TruncationStrategy.SimpleTruncate, config.TruncationStrategy);
            Assert.True(config.LogRequestHeaders);
            Assert.True(config.LogResponseHeaders);
            Assert.True(config.LogRequestBody);
            Assert.True(config.LogResponseBody);
        }

        #endregion

        #region Property Assignment Tests

        [Fact]
        public void ApiLoggingEffectiveConfig_CanModifyAfterCreation()
        {
            // Arrange
            var config = new ApiLoggingEffectiveConfig();

            // Act
            config.Enabled = false;
            config.ExcludeStatusCodes.Add(200);
            config.ExcludedPaths.Add("/test");
            config.SensitiveFields.Add("custom");
            config.MaxRequestBodySize = 1024;
            config.MaxResponseBodySize = 2048;

            // Assert
            Assert.False(config.Enabled);
            Assert.Contains(200, config.ExcludeStatusCodes);
            Assert.Contains("/test", config.ExcludedPaths);
            Assert.Contains("custom", config.SensitiveFields);
            Assert.Equal(1024, config.MaxRequestBodySize);
            Assert.Equal(2048, config.MaxResponseBodySize);
        }

        #endregion
    }
}
