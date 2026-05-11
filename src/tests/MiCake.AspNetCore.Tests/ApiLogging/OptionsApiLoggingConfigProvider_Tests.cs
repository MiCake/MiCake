using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="OptionsApiLoggingConfigProvider"/> implementation.
    /// </summary>
    public class OptionsApiLoggingConfigProvider_Tests
    {
        #region Configuration Loading Tests

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsEnabledFromOptions()
        {
            // Arrange
            var options = CreateOptions(enabled: true);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.True(config.Enabled);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsDisabledFromOptions()
        {
            // Arrange
            var options = CreateOptions(enabled: false);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.False(config.Enabled);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsExcludeStatusCodes()
        {
            // Arrange
            var excludeStatusCodes = new List<int> { 200, 204, 304 };
            var options = CreateOptions(excludeStatusCodes: excludeStatusCodes);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.Equal(excludeStatusCodes.Count, config.ExcludeStatusCodes.Count);
            Assert.Contains(200, config.ExcludeStatusCodes);
            Assert.Contains(204, config.ExcludeStatusCodes);
            Assert.Contains(304, config.ExcludeStatusCodes);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsExcludedPaths()
        {
            // Arrange
            var excludedPaths = new List<string> { "/health", "/metrics", "/api/internal/*" };
            var options = CreateOptions(excludedPaths: excludedPaths);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.Equal(excludedPaths.Count, config.ExcludedPaths.Count);
            Assert.Contains("/health", config.ExcludedPaths);
            Assert.Contains("/metrics", config.ExcludedPaths);
            Assert.Contains("/api/internal/*", config.ExcludedPaths);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsSensitiveFields()
        {
            // Arrange
            var sensitiveFields = new List<string> { "password", "token", "creditCard" };
            var options = CreateOptions(sensitiveFields: sensitiveFields);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.Equal(sensitiveFields.Count, config.SensitiveFields.Count);
            Assert.Contains("password", config.SensitiveFields);
            Assert.Contains("token", config.SensitiveFields);
            Assert.Contains("creditCard", config.SensitiveFields);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsMaxRequestBodySize()
        {
            // Arrange
            var options = CreateOptions(maxRequestBodySize: 8192);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.Equal(8192, config.MaxRequestBodySize);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsMaxResponseBodySize()
        {
            // Arrange
            var options = CreateOptions(maxResponseBodySize: 16384);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.Equal(16384, config.MaxResponseBodySize);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsTruncationStrategy()
        {
            // Arrange
            var options = CreateOptions(truncationStrategy: TruncationStrategy.MetadataOnly);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.Equal(TruncationStrategy.MetadataOnly, config.TruncationStrategy);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsLogRequestHeaders()
        {
            // Arrange
            var options = CreateOptions(logRequestHeaders: true);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.True(config.LogRequestHeaders);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsLogResponseHeaders()
        {
            // Arrange
            var options = CreateOptions(logResponseHeaders: true);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.True(config.LogResponseHeaders);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsLogRequestBody()
        {
            // Arrange
            var options = CreateOptions(logRequestBody: false);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.False(config.LogRequestBody);
        }

        [Fact]
        public async Task GetEffectiveConfigAsync_ReturnsLogResponseBody()
        {
            // Arrange
            var options = CreateOptions(logResponseBody: false);
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.False(config.LogResponseBody);
        }

        #endregion

        #region Caching Tests

        [Fact]
        public async Task GetEffectiveConfigAsync_MultipleCalls_ReturnsCachedConfig()
        {
            // Arrange
            var options = CreateOptions();
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Act
            var config1 = await provider.GetEffectiveConfigAsync();
            var config2 = await provider.GetEffectiveConfigAsync();

            // Assert - Should return the same cached instance
            Assert.Same(config1, config2);
        }

        [Fact]
        public async Task RefreshAsync_ClearsCache()
        {
            // Arrange
            var options = CreateOptions();
            var provider = new OptionsApiLoggingConfigProvider(options);

            // Get initial config (caches it)
            var config1 = await provider.GetEffectiveConfigAsync();

            // Act
            await provider.RefreshAsync();
            var config2 = await provider.GetEffectiveConfigAsync();

            // Assert - Should return a new instance after refresh
            Assert.NotSame(config1, config2);
        }

        #endregion

        #region Default Values Tests

        [Fact]
        public async Task GetEffectiveConfigAsync_WithDefaultOptions_ReturnsDefaultValues()
        {
            // Arrange
            var options = Options.Create(new MiCakeAspNetOptions());
            var optionsMonitor = new TestOptionsMonitor<MiCakeAspNetOptions>(options.Value);
            var provider = new OptionsApiLoggingConfigProvider(optionsMonitor);

            // Act
            var config = await provider.GetEffectiveConfigAsync();

            // Assert
            Assert.True(config.Enabled);
            Assert.Empty(config.ExcludeStatusCodes);
            Assert.Equal(4096, config.MaxRequestBodySize);
            Assert.Equal(4096, config.MaxResponseBodySize);
            Assert.Equal(TruncationStrategy.TruncateWithSummary, config.TruncationStrategy);
        }

        #endregion

        #region Helper Methods

        private static IOptionsMonitor<MiCakeAspNetOptions> CreateOptions(
            bool enabled = true,
            List<int>? excludeStatusCodes = null,
            List<string>? excludedPaths = null,
            List<string>? sensitiveFields = null,
            int maxRequestBodySize = 4096,
            int maxResponseBodySize = 4096,
            TruncationStrategy truncationStrategy = TruncationStrategy.TruncateWithSummary,
            bool logRequestHeaders = false,
            bool logResponseHeaders = false,
            bool logRequestBody = true,
            bool logResponseBody = true)
        {
            var aspNetOptions = new MiCakeAspNetOptions();
            aspNetOptions.ApiLoggingOptions = new ApiLoggingOptions
            {
                Enabled = enabled,
                ExcludeStatusCodes = excludeStatusCodes ?? [],
                ExcludedPaths = excludedPaths ?? ["/health", "/metrics"],
                SensitiveFields = sensitiveFields ?? ["password", "token", "secret", "key", "authorization"],
                MaxRequestBodySize = maxRequestBodySize,
                MaxResponseBodySize = maxResponseBodySize,
                TruncationStrategy = truncationStrategy,
                LogRequestHeaders = logRequestHeaders,
                LogResponseHeaders = logResponseHeaders,
                LogRequestBody = logRequestBody,
                LogResponseBody = logResponseBody
            };

            return new TestOptionsMonitor<MiCakeAspNetOptions>(aspNetOptions);
        }

        #endregion

        #region Test Implementations

        private class TestOptionsMonitor<T> : IOptionsMonitor<T>
        {
            private T _currentValue;
            private event System.Action<T, string>? _onChange;

            public TestOptionsMonitor(T value)
            {
                _currentValue = value;
            }

            public T CurrentValue => _currentValue;

            public T Get(string? name) => _currentValue;

            public IDisposable? OnChange(System.Action<T, string?> listener)
            {
                _onChange += listener!;
                return new ChangeToken(() => _onChange -= listener!);
            }

            public void UpdateValue(T newValue)
            {
                _currentValue = newValue;
                _onChange?.Invoke(newValue, null!);
            }

            private class ChangeToken : IDisposable
            {
                private readonly System.Action _dispose;

                public ChangeToken(System.Action dispose) => _dispose = dispose;

                public void Dispose() => _dispose();
            }
        }

        #endregion
    }
}
