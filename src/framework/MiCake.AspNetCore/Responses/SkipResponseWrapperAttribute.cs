using System;

namespace MiCake.AspNetCore.Responses
{
    /// <summary>
    /// Indicates that the response from an action or controller should not be wrapped by the ResponseWrapper.
    /// Use this attribute when you want to bypass the automatic response wrapping for specific endpoints.
    /// </summary>
    /// <remarks>
    /// This attribute can be applied at the controller level (affecting all actions) 
    /// or at the action level (affecting only that specific action).
    /// When applied at both levels, the action-level attribute takes precedence.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Skip wrapping for a specific action
    /// [SkipResponseWrapper]
    /// public IActionResult GetRawData()
    /// {
    ///     return Ok(new { data = "raw" });
    /// }
    /// 
    /// // Skip wrapping for all actions in a controller
    /// [SkipResponseWrapper]
    /// public class RawDataController : ControllerBase
    /// {
    ///     // All actions in this controller will not be wrapped
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipResponseWrapperAttribute : Attribute
    {
    }
}
