using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Provides dynamic API logging configuration.
    /// <para>
    /// Implement this interface to load configuration from database,
    /// configuration center, or other external sources.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation <see cref="Internals.OptionsApiLoggingConfigProvider"/>
    /// uses <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/> to monitor
    /// configuration changes from appsettings.json.
    /// </para>
    /// <para>
    /// Custom implementations should handle caching appropriately to avoid
    /// excessive database or network calls on every request.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class DatabaseApiLoggingConfigProvider : IApiLoggingConfigProvider
    /// {
    ///     public async Task&lt;ApiLoggingEffectiveConfig&gt; GetEffectiveConfigAsync(CancellationToken ct)
    ///     {
    ///         // Load from database with caching
    ///         return await _cache.GetOrCreateAsync("ApiLogging:Config", async entry =>
    ///         {
    ///             entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
    ///             return await LoadFromDatabaseAsync();
    ///         });
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IApiLoggingConfigProvider
    {
        /// <summary>
        /// Gets the current effective configuration, merging static options
        /// with dynamic overrides.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Effective configuration for API logging</returns>
        Task<ApiLoggingEffectiveConfig> GetEffectiveConfigAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Forces a configuration refresh (e.g., clear cache).
        /// Call this method when configuration has been updated externally.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RefreshAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Effective configuration after merging static and dynamic settings.
    /// This is the resolved configuration used by the logging filter.
    /// </summary>
    public class ApiLoggingEffectiveConfig
    {
        /// <summary>
        /// Whether API logging is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// HTTP status codes to exclude from logging.
        /// </summary>
        public List<int> ExcludeStatusCodes { get; set; } = [];

        /// <summary>
        /// URL paths to exclude from logging.
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = [];

        /// <summary>
        /// Content types to exclude from body logging.
        /// </summary>
        public List<string> ExcludedContentTypes { get; set; } = [];

        /// <summary>
        /// Field names to mask in request/response bodies.
        /// </summary>
        public List<string> SensitiveFields { get; set; } = [];

        /// <summary>
        /// Maximum request body size to log (bytes).
        /// </summary>
        public int MaxRequestBodySize { get; set; } = 4096;

        /// <summary>
        /// Maximum response body size to log (bytes).
        /// </summary>
        public int MaxResponseBodySize { get; set; } = 4096;

        /// <summary>
        /// Strategy for handling large responses.
        /// </summary>
        public TruncationStrategy TruncationStrategy { get; set; } = TruncationStrategy.TruncateWithSummary;

        /// <summary>
        /// Whether to log request headers.
        /// </summary>
        public bool LogRequestHeaders { get; set; } = false;

        /// <summary>
        /// Whether to log response headers.
        /// </summary>
        public bool LogResponseHeaders { get; set; } = false;

        /// <summary>
        /// Whether to log request body.
        /// </summary>
        public bool LogRequestBody { get; set; } = true;

        /// <summary>
        /// Whether to log response body.
        /// </summary>
        public bool LogResponseBody { get; set; } = true;

        /// <summary>
        /// Creates an effective configuration from the given options.
        /// </summary>
        /// <param name="options">The source options</param>
        /// <returns>A new effective configuration instance</returns>
        public static ApiLoggingEffectiveConfig FromOptions(ApiLoggingOptions options)
        {
            return new ApiLoggingEffectiveConfig
            {
                Enabled = options.Enabled,
                ExcludeStatusCodes = [.. options.ExcludeStatusCodes],
                ExcludedPaths = [.. options.ExcludedPaths],
                ExcludedContentTypes = [.. options.ExcludedContentTypes],
                SensitiveFields = [.. options.SensitiveFields],
                MaxRequestBodySize = options.MaxRequestBodySize,
                MaxResponseBodySize = options.MaxResponseBodySize,
                TruncationStrategy = options.TruncationStrategy,
                LogRequestHeaders = options.LogRequestHeaders,
                LogResponseHeaders = options.LogResponseHeaders,
                LogRequestBody = options.LogRequestBody,
                LogResponseBody = options.LogResponseBody
            };
        }
    }
}
