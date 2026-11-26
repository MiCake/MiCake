using System;

namespace MiCake.DDD.Uow.Exceptions
{
    /// <summary>
    /// Raised when a save operation makes no progress or exceeds the configured maximum save cycles.
    /// The owning unit of work is left rollback-only.
    /// </summary>
    public class SaveChangesReentryException : Exception
    {
        public SaveChangesReentryException(string message)
            : base(message)
        {
        }

        public SaveChangesReentryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
