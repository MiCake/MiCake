using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Uow.Exceptions
{
    /// <summary>
    /// Raised when a unit of work commit fails after at least one resource committed.
    /// The unit of work is not rolled back as a whole; inspect <see cref="Outcome"/> for per-resource state.
    /// Contains no sensitive data (no SQL, entity values, or connection strings).
    /// </summary>
    public class PartialUnitOfWorkCommitException : Exception
    {
        /// <summary>Complete per-resource commit outcome.</summary>
        public UnitOfWorkCommitOutcome Outcome { get; }

        /// <summary>Commit failures that occurred before the first rollback attempt.</summary>
        public IReadOnlyList<Exception> CommitFailures { get; }

        /// <summary>Failures raised while rolling back eligible uncommitted resources.</summary>
        public IReadOnlyList<Exception> RollbackFailures { get; }

        public PartialUnitOfWorkCommitException(
            string message,
            UnitOfWorkCommitOutcome outcome,
            IEnumerable<Exception> commitFailures,
            IEnumerable<Exception> rollbackFailures,
            Exception? innerException = null)
            : base(message, innerException)
        {
            Outcome = outcome;
            CommitFailures = commitFailures.ToArray();
            RollbackFailures = rollbackFailures.ToArray();
        }
    }
}
