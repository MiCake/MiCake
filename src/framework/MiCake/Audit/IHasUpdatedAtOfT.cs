using System;

namespace MiCake.Audit
{
    /// <summary>
    /// Define a class has modification time with custom timestamp type.
    /// <para>
    /// This is the generic version that supports <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The timestamp type (DateTime or DateTimeOffset)</typeparam>
    public interface IHasUpdatedAt<T> where T : struct
    {
        /// <summary>
        /// The time when the entity was last modified.
        /// </summary>
        T? UpdatedAt { get; set; }
    }
}
