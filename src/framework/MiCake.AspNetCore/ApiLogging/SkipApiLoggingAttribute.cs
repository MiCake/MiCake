using System;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Indicates that the action or controller should not be logged by the API logging system.
    /// <para>
    /// Use this attribute to bypass API logging for specific endpoints,
    /// such as file downloads, health checks, or high-frequency endpoints.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute can be applied at the controller level (affecting all actions)
    /// or at the action level (affecting only that specific action).
    /// </para>
    /// <para>
    /// When applied at both levels, the action-level attribute takes precedence.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Skip logging for a specific action
    /// [SkipApiLogging]
    /// [HttpGet("download/{id}")]
    /// public async Task&lt;IActionResult&gt; DownloadFile(long id)
    /// {
    ///     return File(stream, "application/octet-stream");
    /// }
    /// 
    /// // Skip logging for all actions in a controller
    /// [SkipApiLogging]
    /// [Route("api/[controller]")]
    /// public class HealthController : ControllerBase
    /// {
    ///     // All actions in this controller will not be logged
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipApiLoggingAttribute : Attribute
    {
    }
}
