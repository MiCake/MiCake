using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Processes and transforms log entries before writing.
    /// <para>
    /// Implement this interface to add custom processing steps to the logging pipeline.
    /// Multiple processors can be registered and will be executed in order of <see cref="Order"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built-in processors include:
    /// <list type="bullet">
    /// <item><description>Sensitive data masking processor</description></item>
    /// <item><description>Body truncation processor</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Custom processors can be added to enrich logs with additional context,
    /// filter out certain entries, or transform the log format.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class UserContextProcessor : IApiLogProcessor
    /// {
    ///     public int Order => 100; // Run after built-in processors
    ///     
    ///     public Task&lt;ApiLogEntry?&gt; ProcessAsync(ApiLogEntry entry, ApiLogProcessingContext context)
    ///     {
    ///         // Add user information to the log entry
    ///         var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    ///         if (userId != null)
    ///         {
    ///             entry.Request.Headers ??= new Dictionary&lt;string, string&gt;();
    ///             entry.Request.Headers["X-User-Id"] = userId;
    ///         }
    ///         return Task.FromResult&lt;ApiLogEntry?&gt;(entry);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IApiLogProcessor
    {
        /// <summary>
        /// Gets the processing order. Lower values execute first.
        /// <para>
        /// Built-in processors use orders 0-50. Custom processors should use 100+ to run after built-in ones.
        /// </para>
        /// </summary>
        int Order => 0;

        /// <summary>
        /// Processes a log entry.
        /// </summary>
        /// <param name="entry">The log entry to process</param>
        /// <param name="context">Processing context containing HTTP context and configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed entry, or null to skip logging this request</returns>
        Task<ApiLogEntry?> ProcessAsync(
            ApiLogEntry entry,
            ApiLogProcessingContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Context information passed to <see cref="IApiLogProcessor"/> implementations.
    /// </summary>
    public class ApiLogProcessingContext
    {
        /// <summary>
        /// The HTTP context for the current request.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// The effective configuration for this request.
        /// </summary>
        public ApiLoggingEffectiveConfig Configuration { get; }

        /// <summary>
        /// Creates a new processing context.
        /// </summary>
        /// <param name="httpContext">The HTTP context</param>
        /// <param name="configuration">The effective configuration</param>
        public ApiLogProcessingContext(HttpContext httpContext, ApiLoggingEffectiveConfig configuration)
        {
            HttpContext = httpContext;
            Configuration = configuration;
        }
    }
}
