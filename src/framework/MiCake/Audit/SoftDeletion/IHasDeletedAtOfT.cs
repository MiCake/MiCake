using System;

namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Define a class has deletion time with custom timestamp type.
    /// <para>
    /// This is the generic version that supports <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The timestamp type (DateTime or DateTimeOffset)</typeparam>
    public interface IHasDeletedAt<T> where T : struct
    {
        /// <summary>
        /// The time when the entity was deleted.
        /// </summary>
        T? DeletedAt { get; set; }
    }
}
