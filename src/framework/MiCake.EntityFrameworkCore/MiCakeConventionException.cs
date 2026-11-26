using System;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// Thrown while the EF Core model is built when a MiCake convention cannot be applied safely,
    /// for example when a soft-deletable entity already has an anonymous query filter.
    /// Failures of this type are never logged and ignored.
    /// </summary>
    public class MiCakeConventionException : InvalidOperationException
    {
        /// <summary>
        /// Creates the exception.
        /// </summary>
        /// <param name="message">What went wrong and how to fix it.</param>
        /// <param name="entityType">The entity type the convention was applied to.</param>
        /// <param name="innerException">The underlying failure, if any.</param>
        public MiCakeConventionException(string message, Type? entityType = null, Exception? innerException = null)
            : base(message, innerException)
        {
            EntityType = entityType;
        }

        /// <summary>
        /// The entity type the convention was applied to, when known.
        /// </summary>
        public Type? EntityType { get; }
    }
}
