using System;

namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Define a class has deletion time.
    /// </summary>
    public interface IHasDeletedAt
    {
        /// <summary>
        /// The time when the entity was deleted.
        /// </summary>
        DateTime? DeletedAt { get; set; }
    }
}
