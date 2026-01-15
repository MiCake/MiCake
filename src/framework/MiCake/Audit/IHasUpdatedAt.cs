using System;

namespace MiCake.Audit
{
    /// <summary>
    /// Define a class has modification time.
    /// </summary>
    public interface IHasUpdatedAt
    {
        /// <summary>
        /// The time when the entity was last modified.
        /// </summary>
        DateTime? UpdatedAt { get; set; }
    }
}
