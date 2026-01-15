using System;
using System.Collections.Generic;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Represents a complete API log entry containing request and response information.
    /// </summary>
    public class ApiLogEntry
    {
        /// <summary>
        /// Unique correlation ID for tracing request-response pairs.
        /// Typically derived from <see cref="Microsoft.AspNetCore.Http.HttpContext.TraceIdentifier"/>.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the request was received.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Request information.
        /// </summary>
        public ApiRequestLog Request { get; set; } = new();

        /// <summary>
        /// Response information (populated after action execution).
        /// </summary>
        public ApiResponseLog Response { get; set; } = new();

        /// <summary>
        /// Total elapsed time in milliseconds from request received to response sent.
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// Request-specific log information.
    /// </summary>
    public class ApiRequestLog
    {
        /// <summary>
        /// HTTP method (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Request path (e.g., "/api/users/123").
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Query string portion of the URL (e.g., "?page=1&amp;size=10").
        /// </summary>
        public string? QueryString { get; set; }

        /// <summary>
        /// Request body content (may be truncated or masked).
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Request headers (only populated if <see cref="ApiLoggingOptions.LogRequestHeaders"/> is true).
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Content-Type header value.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Content-Length header value in bytes.
        /// </summary>
        public long? ContentLength { get; set; }
    }

    /// <summary>
    /// Response-specific log information.
    /// </summary>
    public class ApiResponseLog
    {
        /// <summary>
        /// HTTP status code (e.g., 200, 404, 500).
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Response body content (may be truncated or masked).
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Response headers (only populated if <see cref="ApiLoggingOptions.LogResponseHeaders"/> is true).
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Content-Type header value.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Content-Length header value in bytes.
        /// </summary>
        public long? ContentLength { get; set; }

        /// <summary>
        /// Indicates whether the body was truncated due to size limits.
        /// </summary>
        public bool IsTruncated { get; set; }

        /// <summary>
        /// Original body size in bytes before truncation.
        /// Only set when <see cref="IsTruncated"/> is true.
        /// </summary>
        public long? OriginalSize { get; set; }

        /// <summary>
        /// Summary information for truncated content.
        /// For JSON arrays, may include item count (e.g., "Array with 500 items").
        /// </summary>
        public string? TruncationSummary { get; set; }
    }
}
