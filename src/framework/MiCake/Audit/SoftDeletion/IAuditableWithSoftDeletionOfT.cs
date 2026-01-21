using System;

namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Mark a class with audit timestamps and soft deletion properties using DateTime or DateTimeOffset.
    /// <para>
    /// This is the generic version that supports <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.
    /// </para>
    /// <para>
    /// It is the combination of <see cref="IHasAuditTimestamps{T}"/>, <see cref="ISoftDeletable"/>, and <see cref="IHasDeletedAt{T}"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The timestamp type (DateTime or DateTimeOffset)</typeparam>
    public interface IAuditableWithSoftDeletion<T> : IHasAuditTimestamps<T>, ISoftDeletable, IHasDeletedAt<T>
        where T : struct
    {
    }
}
