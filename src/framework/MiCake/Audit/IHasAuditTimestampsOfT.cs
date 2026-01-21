using System;

namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with the timestamps of creation and modification using custom timestamp type.
    /// <para>
    /// This is the generic version that supports <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.
    /// </para>
    /// <para>
    /// It is the combination of <see cref="IHasCreatedAt{T}"/> and <see cref="IHasUpdatedAt{T}"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The timestamp type (DateTime or DateTimeOffset)</typeparam>
    public interface IHasAuditTimestamps<T> : IHasCreatedAt<T>, IHasUpdatedAt<T>
        where T : struct
    {
    }
}
