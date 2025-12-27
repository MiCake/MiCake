using System.Collections.Generic;

namespace MiCake.AspNetCore.ApiLogging
{
    /// <summary>
    /// Masks sensitive data in request/response bodies.
    /// <para>
    /// Implement this interface to customize how sensitive fields are detected and masked.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation handles JSON content by detecting and masking
    /// field values whose names match the configured sensitive field patterns.
    /// </para>
    /// <para>
    /// Masked values are replaced with "***" by default.
    /// </para>
    /// </remarks>
    public interface ISensitiveDataMasker
    {
        /// <summary>
        /// Masks sensitive fields in the given content.
        /// </summary>
        /// <param name="content">Original content (typically JSON)</param>
        /// <param name="sensitiveFields">Field names to mask (case-insensitive matching)</param>
        /// <returns>Content with sensitive values replaced by mask characters</returns>
        string Mask(string content, IEnumerable<string> sensitiveFields);
    }
}
