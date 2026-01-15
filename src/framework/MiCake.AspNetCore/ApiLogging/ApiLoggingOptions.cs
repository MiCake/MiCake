using System.Collections.Generic;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Configuration options for API request/response logging.
    /// </summary>
    public class ApiLoggingOptions
    {
        /// <summary>
        /// Whether API logging is enabled.
        /// Default: true
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// HTTP status codes to exclude from logging.
        /// Empty list means log all status codes.
        /// Default: [] (empty, logs all status codes)
        /// </summary>
        /// <remarks>
        /// <para>
        /// This setting can be dynamically overridden by <see cref="IApiLoggingConfigProvider"/>.
        /// </para>
        /// <para>
        /// Example: To exclude successful responses, set to [200, 201, 204].
        /// </para>
        /// </remarks>
        public List<int> ExcludeStatusCodes { get; set; } = [];

        /// <summary>
        /// Maximum request body size to log (bytes).
        /// Content exceeding this limit will be truncated.
        /// Default: 4096 (4KB)
        /// </summary>
        public int MaxRequestBodySize { get; set; } = 4096;

        /// <summary>
        /// Maximum response body size to log (bytes).
        /// Content exceeding this limit will be handled according to <see cref="TruncationStrategy"/>.
        /// Default: 4096 (4KB)
        /// </summary>
        public int MaxResponseBodySize { get; set; } = 4096;

        /// <summary>
        /// Strategy for handling responses that exceed <see cref="MaxResponseBodySize"/>.
        /// Default: TruncateWithSummary
        /// </summary>
        public TruncationStrategy TruncationStrategy { get; set; } = TruncationStrategy.TruncateWithSummary;

        /// <summary>
        /// URL paths to exclude from logging.
        /// Supports glob patterns (e.g., "/api/file/*").
        /// Default: ["/health", "/metrics"]
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = ["/health", "/metrics"];

        /// <summary>
        /// Content types to exclude from body logging.
        /// Supports wildcard patterns (e.g., "image/*").
        /// Default: ["application/octet-stream", "image/*", "video/*"]
        /// </summary>
        public List<string> ExcludedContentTypes { get; set; } =
            ["application/octet-stream", "image/*", "video/*"];

        /// <summary>
        /// Field names to mask in request/response bodies.
        /// Matching is case-insensitive.
        /// Default: ["authorization"]
        /// </summary>
        public List<string> SensitiveFields { get; set; } = ["authorization"];

        /// <summary>
        /// Whether to log request headers.
        /// Default: false
        /// </summary>
        public bool LogRequestHeaders { get; set; } = false;

        /// <summary>
        /// Whether to log response headers.
        /// Default: false
        /// </summary>
        public bool LogResponseHeaders { get; set; } = false;

        /// <summary>
        /// Whether to log request body.
        /// Default: true
        /// </summary>
        public bool LogRequestBody { get; set; } = true;

        /// <summary>
        /// Whether to log response body.
        /// Default: true
        /// </summary>
        public bool LogResponseBody { get; set; } = true;
    }

    /// <summary>
    /// Strategy for truncating large request/response bodies.
    /// </summary>
    public enum TruncationStrategy
    {
        /// <summary>
        /// Simple truncation at the specified size limit.
        /// The truncated content will end with "[truncated]".
        /// </summary>
        SimpleTruncate,

        /// <summary>
        /// Truncate and append a structure summary.
        /// For JSON arrays, includes item count (e.g., "Array with 500 items").
        /// For JSON objects, includes property names.
        /// </summary>
        TruncateWithSummary,

        /// <summary>
        /// Only log metadata (content type, size) without body content.
        /// Useful for binary or very large responses.
        /// </summary>
        MetadataOnly
    }
}
