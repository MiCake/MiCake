using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Context passed to <see cref="IUnitOfWorkResource.Prepare"/> describing the owning unit of work.
    /// </summary>
    public sealed record UnitOfWorkResourceContext(
        Guid UnitOfWorkId,
        IsolationLevel? IsolationLevel,
        bool IsReadOnly);

    /// <summary>
    /// Generic provider resource lifecycle contract for participating in a MiCake unit of work.
    /// The unit of work is the sole persistence owner; resources supply persistence-specific behavior.
    /// </summary>
    public interface IUnitOfWorkResource : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Unique identity of this resource instance.
        /// Generated once per wrapper instance and never derived from <see cref="object.GetHashCode"/>.
        /// </summary>
        UnitOfWorkResourceId Id { get; }

        /// <summary>
        /// Stable type name used in diagnostics and commit outcomes (for example the DbContext type name).
        /// </summary>
        string ResourceType { get; }

        /// <summary>
        /// Indicates whether the resource currently holds an active transaction.
        /// </summary>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Indicates whether the resource supports savepoints.
        /// </summary>
        bool SupportsSavepoints { get; }

        /// <summary>
        /// Prepares the resource for participation in the given unit of work.
        /// Idempotent only for the same unit of work; rebinding to another live unit of work is rejected.
        /// Must not perform I/O.
        /// </summary>
        void Prepare(UnitOfWorkResourceContext context);

        /// <summary>
        /// Synchronously ensures an active transaction. Idempotent.
        /// </summary>
        void EnsureTransaction();

        /// <summary>
        /// Asynchronously ensures an active transaction. Idempotent.
        /// </summary>
        ValueTask EnsureTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes pending changes to the data store without committing.
        /// Legal only after prepare and transaction activation.
        /// </summary>
        ValueTask<int> FlushAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the transaction. Terminal and at most once.
        /// </summary>
        ValueTask CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction. Terminal and at most once.
        /// </summary>
        ValueTask RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a savepoint. Throws <see cref="NotSupportedException"/> before changing state
        /// when <see cref="SupportsSavepoints"/> is false.
        /// </summary>
        ValueTask CreateSavepointAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back to a savepoint. Throws <see cref="NotSupportedException"/> before changing state
        /// when <see cref="SupportsSavepoints"/> is false.
        /// </summary>
        ValueTask RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases a savepoint. Throws <see cref="NotSupportedException"/> before changing state
        /// when <see cref="SupportsSavepoints"/> is false.
        /// </summary>
        ValueTask ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);
    }
}
