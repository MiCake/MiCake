using MiCake.AspNetCore.ApiLogging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.MiCakeFeatures
{
    /// <summary>
    /// Custom API log writer that demonstrates how to implement <see cref="IApiLogWriter"/>.
    /// <para>
    /// This implementation writes API logs to the application's logging system (ILogger).
    /// You can customize this to write logs to databases, files, Elasticsearch, or other destinations.
    /// </para>
    /// </summary>
    /// <remarks>
    /// To use this custom log writer, register it in the DI container:
    /// <code>
    /// services.AddSingleton&lt;IApiLogWriter, ConsoleApiLogWriter&gt;();
    /// </code>
    /// </remarks>
    public class ConsoleApiLogWriter : IApiLogWriter
    {
        private readonly ILogger<ConsoleApiLogWriter> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleApiLogWriter"/> class.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public ConsoleApiLogWriter(ILogger<ConsoleApiLogWriter> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Writes the API log entry to the console/logging system.
        /// </summary>
        /// <param name="entry">The log entry containing request/response information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default)
        {
            // Format the log entry as a structured log message
            var logLevel = GetLogLevel(entry.Response.StatusCode);

            _logger.Log(
                logLevel,
                "[API] {Method} {Path} -> {StatusCode} ({ElapsedMs}ms) | CorrelationId: {CorrelationId}",
                entry.Request.Method,
                entry.Request.Path,
                entry.Response.StatusCode,
                entry.ElapsedMilliseconds,
                entry.CorrelationId);

            // For debugging purposes, also log the full entry as JSON
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var json = JsonSerializer.Serialize(entry, _jsonOptions);
                _logger.LogDebug("[API Detail] {LogEntry}", json);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines the appropriate log level based on HTTP status code.
        /// </summary>
        private static LogLevel GetLogLevel(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => LogLevel.Error,      // Server errors
                >= 400 => LogLevel.Warning,    // Client errors
                _ => LogLevel.Information       // Success responses
            };
        }
    }
}
