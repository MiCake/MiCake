using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MiCake.DDD.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Immutable pre-save state of one changed entity, retained for post-save handlers
    /// even after EF Core mutates the entry state during persistence.
    /// </summary>
    internal readonly record struct EntityStateSnapshot(EntityEntry Entry, RepositoryEntityStates State);

    /// <summary>
    /// Per-DbContext root save-operation frame. One frame is active for the duration of a
    /// root save operation; nested SaveChanges calls from lifecycle handlers are suppressed
    /// and recorded as re-entry requests instead of recursing.
    /// </summary>
    internal sealed class SaveOperationFrame
    {
        public SaveOperationFrame(IServiceProvider handlerProvider, IReadOnlyList<EntityStateSnapshot> snapshots)
        {
            HandlerProvider = handlerProvider;
            Snapshots = snapshots;
        }

        /// <summary>The provider of the scope that owns the unit of work; handlers are resolved from it.</summary>
        public IServiceProvider HandlerProvider { get; }

        public IReadOnlyList<EntityStateSnapshot> Snapshots { get; set; }

        /// <summary>Set when a nested SaveChanges was suppressed during the active root operation.</summary>
        public bool ReentryRequested { get; set; }

        /// <summary>
        /// Set when a suppressed nested SaveChanges found pending tracker changes at the
        /// time of the request; those changes are absorbed by the current save, so a later
        /// empty re-entry request is quiescence rather than a no-progress failure.
        /// </summary>
        public bool LastReentryHadChanges { get; set; }

        /// <summary>Set while the root coordinator drives a follow-up save cycle; the interceptor skips guard/pre-save work.</summary>
        public bool RootCycleSave { get; set; }

        /// <summary>Number of nested SaveChanges calls suppressed while the root operation is active.</summary>
        public int SuppressedSaveCount { get; set; }

        public int CycleCount { get; set; }
    }

    /// <summary>
    /// Per-DbContext save-operation state accessor registered through the EF Core options
    /// extension. Owns the root operation frame and implements the EF Core pool reset contract
    /// so a pooled context clears all operation state before returning to the pool.
    /// </summary>
    internal sealed class SaveOperationStateAccessor : IResettableService
    {
        private SaveOperationFrame? _frame;

        public bool IsOperationActive => _frame != null;

        public SaveOperationFrame? Current => _frame;

        /// <summary>
        /// Starts a root save operation. Returns false when a root operation is already active
        /// (a nested SaveChanges), in which case the caller must request re-entry instead.
        /// </summary>
        public bool TryBeginRoot(IServiceProvider handlerProvider, IReadOnlyList<EntityStateSnapshot> snapshots)
        {
            if (_frame != null)
            {
                return false;
            }

            _frame = new SaveOperationFrame(handlerProvider, snapshots);
            return true;
        }

        public void RequestReentry(bool hadPendingChanges)
        {
            if (_frame != null)
            {
                _frame.ReentryRequested = true;
                _frame.LastReentryHadChanges |= hadPendingChanges;
            }
        }

        public (bool Requested, bool HadChanges) ConsumeReentryRequest()
        {
            if (_frame == null)
            {
                return (false, false);
            }

            var requested = _frame.ReentryRequested;
            var hadChanges = _frame.LastReentryHadChanges;
            _frame.ReentryRequested = false;
            _frame.LastReentryHadChanges = false;
            return (requested, hadChanges);
        }

        public void MarkRootCycleSave()
        {
            if (_frame != null)
            {
                _frame.RootCycleSave = true;
            }
        }

        public bool ConsumeRootCycleSave()
        {
            if (_frame == null)
            {
                return false;
            }

            var isRootCycle = _frame.RootCycleSave;
            _frame.RootCycleSave = false;
            return isRootCycle;
        }

        /// <summary>
        /// Records a suppressed nested SaveChanges so its own completed callback is a no-op.
        /// </summary>
        public void MarkSuppressedSave()
        {
            if (_frame != null)
            {
                _frame.SuppressedSaveCount++;
            }
        }

        /// <summary>
        /// Consumes one suppressed-save marker. Returns true when the current completed
        /// callback belongs to a suppressed nested SaveChanges and must not run post-save work.
        /// </summary>
        public bool ConsumeSuppressedSave()
        {
            if (_frame == null || _frame.SuppressedSaveCount == 0)
            {
                return false;
            }

            _frame.SuppressedSaveCount--;
            return true;
        }

        /// <summary>
        /// Ends the active root operation. All success, failure, cancellation, disposal, and
        /// pool-reset paths converge here.
        /// </summary>
        public void EndOperation()
        {
            _frame = null;
        }

        public void ResetState()
        {
            EndOperation();
        }

        public Task ResetStateAsync(CancellationToken cancellationToken = default)
        {
            EndOperation();
            return Task.CompletedTask;
        }
    }
}
