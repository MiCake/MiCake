using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiCake.AspNetCore.ApiLogging.Internals
{
    /// <summary>
    /// Default implementation of <see cref="IApiLogWriter"/> that uses
    /// <see cref="ILogger"/> to write structured log entries.
    /// </summary>
    internal sealed class MicrosoftLoggerApiLogWriter : IApiLogWriter
    {
        private readonly ILogger<MicrosoftLoggerApiLogWriter> _logger;

        public MicrosoftLoggerApiLogWriter(ILogger<MicrosoftLoggerApiLogWriter> logger)
        {
            _logger = logger;
        }

        public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
        {
            var logLevel = DetermineLogLevel(entry.Response.StatusCode);

            _logger.Log(
                logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms | Request: {RequestBody} | Response: {ResponseBody}{TruncationInfo}",
                entry.Request.Method,
                entry.Request.Path + entry.Request.QueryString,
                entry.Response.StatusCode,
                entry.ElapsedMilliseconds,
                entry.Request.Body ?? "(none)",
                entry.Response.Body ?? "(none)",
                FormatTruncationInfo(entry.Response));

            return Task.CompletedTask;
        }

        private static LogLevel DetermineLogLevel(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };
        }

        private static string FormatTruncationInfo(ApiResponseLog response)
        {
            if (!response.IsTruncated)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append(" | [Truncated");

            if (response.OriginalSize.HasValue)
            {
                sb.Append($": {response.OriginalSize} bytes");
            }

            if (!string.IsNullOrEmpty(response.TruncationSummary))
            {
                sb.Append($", {response.TruncationSummary}");
            }

            sb.Append(']');
            return sb.ToString();
        }
    }
}
