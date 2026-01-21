using System;

namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Mark a class with audit timestamps and soft deletion properties.
    /// <para>
    /// This interface inherits from <see cref="IAuditableWithSoftDeletion{T}"/> with <see cref="DateTime"/> type.
    /// Consider using <see cref="IAuditableWithSoftDeletion{T}"/> directly with <see cref="DateTimeOffset"/> for better timezone support.
    /// </para>
    /// </summary>
    public interface IAuditableWithSoftDeletion : IAuditableWithSoftDeletion<DateTime>
    {
    }
}
