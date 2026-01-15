using System;

namespace MiCake.Audit
{
    /// <summary>
    /// Define a class has creation time.
    /// </summary>
    public interface IHasCreatedAt
    {
        /// <summary>
        /// The time when the entity was created.
        /// </summary>
        DateTime CreatedAt { get; set; }
    }
}
