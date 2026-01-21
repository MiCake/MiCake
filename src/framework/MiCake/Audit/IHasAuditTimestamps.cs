using System;

namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with the timestamps of creation and modification.
    /// <para>
    /// This interface inherits from <see cref="IHasAuditTimestamps{T}"/> with <see cref="DateTime"/> type.
    /// Consider using <see cref="IHasAuditTimestamps{T}"/> directly with <see cref="DateTimeOffset"/> for better timezone support.
    /// </para>
    /// </summary>
    public interface IHasAuditTimestamps : IHasAuditTimestamps<DateTime>
    {
    }
}
