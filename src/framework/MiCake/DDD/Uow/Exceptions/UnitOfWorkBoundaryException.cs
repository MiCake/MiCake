using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Uow.Exceptions
{
    /// <summary>
    /// Raised at a unit of work boundary when a primary failure is combined with rollback or cleanup failures.
    /// Carries every cause without losing the original exception.
    /// </summary>
    public class UnitOfWorkBoundaryException : Exception
    {
        /// <summary>The exception that triggered the boundary rollback, if any.</summary>
        public Exception? PrimaryException { get; }

        /// <summary>Failures raised while rolling back eligible resources.</summary>
        public IReadOnlyList<Exception> RollbackExceptions { get; }

        /// <summary>Failures raised while cleaning up resources.</summary>
        public IReadOnlyList<Exception> CleanupExceptions { get; }

        public UnitOfWorkBoundaryException(
            string message,
            Exception? primaryException = null,
            IEnumerable<Exception>? rollbackExceptions = null,
            IEnumerable<Exception>? cleanupExceptions = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            PrimaryException = primaryException;
            RollbackExceptions = rollbackExceptions?.ToArray() ?? [];
            CleanupExceptions = cleanupExceptions?.ToArray() ?? [];
        }

        /// <summary>
        /// Creates a boundary exception for a failed operation whose rollback also failed.
        /// </summary>
        public static UnitOfWorkBoundaryException ForRollbackFailure(
            Exception primary,
            IReadOnlyList<Exception> rollbackFailures)
            => new(
                "The unit of work operation failed and rollback of eligible resources also failed.",
                primary,
                rollbackFailures);

        /// <summary>
        /// Creates a boundary exception for an explicit rollback that failed.
        /// </summary>
        public static UnitOfWorkBoundaryException ForRollbackFailureOnly(
            IReadOnlyList<Exception> rollbackFailures)
            => new(
                "Rollback of eligible unit of work resources failed.",
                rollbackExceptions: rollbackFailures);
    }
}
