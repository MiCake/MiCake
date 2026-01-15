using System;
using Microsoft.AspNetCore.Http;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Factory for creating log entries with custom logic.
    /// <para>
    /// Implement this interface for complete control over log entry creation.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation creates entries with standard fields populated
    /// from the HTTP context. Custom implementations can add additional fields,
    /// modify the structure, or integrate with external systems.
    /// </para>
    /// <para>
    /// This factory is called early in the request pipeline to create the initial
    /// log entry. The entry is then passed through <see cref="IApiLogProcessor"/>
    /// implementations before being written.
    /// </para>
    /// </remarks>
    public interface IApiLogEntryFactory
    {
        /// <summary>
        /// Creates a log entry from the HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context for the current request</param>
        /// <param name="configuration">The effective logging configuration</param>
        /// <returns>A new log entry instance</returns>
        ApiLogEntry CreateEntry(HttpContext httpContext, ApiLoggingEffectiveConfig configuration);

        /// <summary>
        /// Populates response information in an existing log entry.
        /// </summary>
        /// <param name="entry">The log entry to populate</param>
        /// <param name="httpContext">The HTTP context for the current request</param>
        /// <param name="configuration">The effective logging configuration</param>
        /// <param name="responseBody">The captured response body content</param>
        /// <param name="elapsed">Time elapsed since request start</param>
        void PopulateResponse(
            ApiLogEntry entry,
            HttpContext httpContext,
            ApiLoggingEffectiveConfig configuration,
            string? responseBody,
            TimeSpan elapsed);
    }
}
