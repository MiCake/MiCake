using System;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Logs the full response body for the marked action, ignoring size limits.
    /// <para>
    /// Use this attribute when you need to capture the complete response body
    /// for debugging or auditing purposes, regardless of <see cref="ApiLoggingOptions.MaxResponseBodySize"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Warning: Use this attribute sparingly as it may capture large response bodies
    /// which can impact memory usage and log storage.
    /// </para>
    /// <para>
    /// The <see cref="MaxSize"/> property can be used to set a custom maximum size
    /// that differs from the global setting.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Log full response body (no size limit)
    /// [LogFullResponse]
    /// [HttpGet("debug/{id}")]
    /// public async Task&lt;IActionResult&gt; GetDebugInfo(long id)
    /// {
    ///     return Ok(debugData);
    /// }
    /// 
    /// // Log up to 64KB of response body
    /// [LogFullResponse(MaxSize = 65536)]
    /// [HttpGet("export")]
    /// public async Task&lt;IActionResult&gt; ExportData()
    /// {
    ///     return Ok(exportData);
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class LogFullResponseAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the maximum response body size to log (in bytes).
        /// <para>
        /// If set to 0 or negative, no size limit is applied.
        /// If positive, this value overrides <see cref="ApiLoggingOptions.MaxResponseBodySize"/>.
        /// </para>
        /// Default: 0 (no limit)
        /// </summary>
        public int MaxSize { get; set; } = 0;
    }
}
