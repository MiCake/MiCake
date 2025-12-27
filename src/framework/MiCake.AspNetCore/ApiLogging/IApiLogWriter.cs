using System.Threading;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Writes log entries to the destination.
    /// <para>
    /// Implement this interface to customize where and how log entries are written.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation uses <see cref="Microsoft.Extensions.Logging.ILogger"/>
    /// to write structured logs that integrate with any configured logging provider
    /// (Console, File, Seq, Application Insights, etc.).
    /// </para>
    /// <para>
    /// Custom implementations can write to databases, message queues, or external services.
    /// Consider implementing async/batch writing for high-throughput scenarios.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ElasticsearchApiLogWriter : IApiLogWriter
    /// {
    ///     public async Task WriteAsync(ApiLogEntry entry, CancellationToken ct)
    ///     {
    ///         await _elasticClient.IndexAsync(entry, idx => idx
    ///             .Index($"api-logs-{DateTime.UtcNow:yyyy.MM.dd}")
    ///             .Id(entry.CorrelationId), ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IApiLogWriter
    {
        /// <summary>
        /// Writes a log entry to the destination.
        /// </summary>
        /// <param name="entry">The log entry to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous write operation</returns>
        Task WriteAsync(ApiLogEntry entry, CancellationToken cancellationToken = default);
    }
}
