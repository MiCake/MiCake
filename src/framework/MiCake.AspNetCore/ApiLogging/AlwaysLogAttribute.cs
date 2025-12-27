using System;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Forces API logging for the marked action, ignoring status code exclusions.
    /// <para>
    /// Use this attribute when you want to ensure an action is always logged,
    /// regardless of <see cref="ApiLoggingOptions.ExcludeStatusCodes"/> settings.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute is useful for critical operations where you want to ensure
    /// a complete audit trail, even for successful responses that might normally
    /// be excluded from logging.
    /// </para>
    /// <para>
    /// This attribute takes precedence over <see cref="ApiLoggingOptions.ExcludeStatusCodes"/>
    /// but can be overridden by <see cref="SkipApiLoggingAttribute"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Always log this action, even if 200 is in ExcludeStatusCodes
    /// [AlwaysLog]
    /// [HttpPost]
    /// public async Task&lt;IActionResult&gt; CreateOrder([FromBody] CreateOrderRequest request)
    /// {
    ///     // Critical operation - always needs audit trail
    ///     return Ok(result);
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AlwaysLogAttribute : Attribute
    {
    }
}
