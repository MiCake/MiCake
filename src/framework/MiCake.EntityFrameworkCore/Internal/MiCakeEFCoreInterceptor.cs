using MiCake.Core.DependencyInjection;
using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.DDD.Uow.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// EF Core interceptor for MiCake repository lifecycle events.
    /// Drives the per-DbContext root save-operation state machine: the first SaveChanges
    /// becomes the root operation (guard, snapshot, pre-save handlers), nested SaveChanges
    /// from lifecycle handlers are suppressed and recorded as re-entry requests, and the
    /// root coordinator re-scans changes into bounded follow-up save cycles in the same
    /// transaction. Handlers are always resolved from the provider of the scope that owns
    /// the unit of work.
    /// </summary>
    internal class MiCakeEFCoreInterceptor : ISaveChangesInterceptor
    {
        private readonly ILogger<MiCakeEFCoreInterceptor> _logger;
        private readonly IUnitOfWorkAmbientAccessor _ambientAccessor;

        public MiCakeEFCoreInterceptor(
            ILogger<MiCakeEFCoreInterceptor> logger,
            IUnitOfWorkAmbientAccessor ambientAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ambientAccessor = ambientAccessor ?? throw new ArgumentNullException(nameof(ambientAccessor));
        }

        public void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            var accessor = ResolveAccessor(eventData.Context);
            if (accessor != null && accessor.IsOperationActive)
            {
                _logger.LogDebug("Ending save operation for {ContextType} after a save failure", eventData.Context?.GetType().Name);
                accessor.EndOperation();
            }
        }

        public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            SaveChangesFailed(eventData);
            return Task.CompletedTask;
        }

        public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            _logger.LogWarning(
                "Synchronous SaveChanges detected in {ContextType}. " +
                "This may cause deadlocks in .NET Core applications. " +
                "Please use SaveChangesAsync instead.",
                eventData.Context?.GetType().Name);

            try
            {
                return SavedChangesAsync(eventData, result, default)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in synchronous SavedChanges for {ContextType}",
                    eventData.Context?.GetType().Name);
                throw;
            }
        }

        public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null)
            {
                return result;
            }

            var accessor = ResolveAccessor(context);
            if (accessor == null || !accessor.IsOperationActive)
            {
                return result;
            }

            if (accessor.ConsumeSuppressedSave())
            {
                // This completed callback belongs to a nested SaveChanges that was suppressed
                // in SavingChanges; the root operation's own callback drives post-save work.
                return result;
            }

            try
            {
                var maxSaveCycles = ResolveMaxSaveCycles(context);

                while (true)
                {
                    var frame = accessor.Current;
                    if (frame == null)
                    {
                        // A nested root-cycle SaveChanges completed and ended the operation;
                        // its SavedChanges already ran post-save handling and re-entry checks.
                        break;
                    }

                    // Post-save handlers receive the pre-save repository states from the snapshot.
                    await RunPostSaveAsync(frame.Snapshots, frame.HandlerProvider, cancellationToken).ConfigureAwait(false);

                    frame.CycleCount++;

                    var (reentryRequested, reentryHadChanges) = accessor.ConsumeReentryRequest();
                    var hasPendingChanges = HasChangedEntries(context);
                    if (!reentryRequested && !hasPendingChanges)
                    {
                        break;
                    }

                    if (reentryRequested && !hasPendingChanges)
                    {
                        if (!reentryHadChanges)
                        {
                            throw new SaveChangesReentryException(
                                $"Save operation on {context.GetType().Name} received a re-entry request without new pending changes; " +
                                "no progress is possible. The unit of work is left rollback-only; check lifecycle handlers that " +
                                "call SaveChanges without modifying the tracker.");
                        }

                        // The re-entry's pending changes were absorbed by the current save,
                        // so the operation is quiescent.
                        break;
                    }

                    if (frame.CycleCount >= maxSaveCycles)
                    {
                        throw new SaveChangesReentryException(
                            $"Save operation on {context.GetType().Name} exceeded the configured maximum of {maxSaveCycles} " +
                            "save cycles while handling re-entry requests. The unit of work is left rollback-only; " +
                            "check lifecycle handlers for unbounded change generation.");
                    }

                    // Follow-up cycle: rescan changes, run pre-save handlers, then save again in the same transaction.
                    var entries = GetChangedEntities(context);
                    var entriesByType = SaveOperationEntityHelper.BuildEntriesByType(context);
                    var changedOwnedOwners = SaveOperationEntityHelper.BuildChangedOwnedOwners(context, entriesByType);
                    frame.Snapshots = entries
                        .Select(e => new EntityStateSnapshot(e, ResolvePreSaveState(e, entriesByType, changedOwnedOwners)))
                        .ToArray();

                    await RunPreSaveCycleAsync(entries, entriesByType, changedOwnedOwners, frame.HandlerProvider, cancellationToken).ConfigureAwait(false);

                    accessor.MarkRootCycleSave();
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                accessor.EndOperation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save operation failed for {ContextType}; ending the operation", context.GetType().Name);
                var handlerProvider = accessor.Current?.HandlerProvider;
                accessor.EndOperation();

                if (ex is SaveChangesReentryException && handlerProvider != null)
                {
                    MarkUnitOfWorkRollbackOnly(handlerProvider, context);
                }

                throw;
            }

            return result;
        }

        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            _logger.LogWarning(
                "Synchronous SaveChanges detected in {ContextType}. " +
                "This may cause deadlocks in .NET Core applications. " +
                "Please use SaveChangesAsync instead.",
                eventData.Context?.GetType().Name);

            try
            {
                // Permissive: no ambient UoW -> pass through (native EF).
                if (eventData.Context == null || MiCakeInterceptorPipeline.ResolveCoordinator(_ambientAccessor) == null)
                {
                    return result;
                }

                RequireCoordinator(eventData.Context).BeforeWrite(eventData.Context, EFWriteOperationKind.SaveChanges);
                return SavingChangesCore(eventData, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in synchronous SavingChanges for {ContextType}",
                    eventData.Context?.GetType().Name);
                ResolveAccessor(eventData.Context)?.EndOperation();
                throw;
            }
        }

        public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            // Permissive: no ambient UoW -> pass through (native EF).
            if (eventData.Context == null || MiCakeInterceptorPipeline.ResolveCoordinator(_ambientAccessor) == null)
            {
                return result;
            }

            await RequireCoordinator(eventData.Context)
                .BeforeWriteAsync(eventData.Context, EFWriteOperationKind.SaveChanges, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                return await SavingChangesCoreAsync(eventData, result, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavingChangesAsync for {ContextType}", eventData.Context!.GetType().Name);
                // EF Core does not raise SaveChangesFailed when the failure originates in
                // this interceptor, so the operation frame must be ended here.
                ResolveAccessor(eventData.Context)?.EndOperation();
                throw;
            }
        }

        private InterceptionResult<int> SavingChangesCore(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var context = eventData.Context!;
            var accessor = ResolveAccessor(context);
            if (accessor == null)
            {
                return result;
            }

            if (accessor.ConsumeRootCycleSave())
            {
                return result;
            }

            if (accessor.IsOperationActive)
            {
                accessor.RequestReentry(HasChangedEntries(context));
                accessor.MarkSuppressedSave();
                return InterceptionResult<int>.SuppressWithResult(0);
            }

            var entries = GetChangedEntities(context);
            var entriesByType = SaveOperationEntityHelper.BuildEntriesByType(context);
            var changedOwnedOwners = SaveOperationEntityHelper.BuildChangedOwnedOwners(context, entriesByType);
            var snapshots = entries
                .Select(e => new EntityStateSnapshot(e, ResolvePreSaveState(e, entriesByType, changedOwnedOwners)))
                .ToArray();

            if (snapshots.Length == 0)
            {
                return result;
            }

            var handlerProvider = ResolveHandlerProvider();
            if (handlerProvider == null)
            {
                return result;
            }

            if (!accessor.TryBeginRoot(handlerProvider, snapshots))
            {
                return result;
            }

            RunPreSaveCycleAsync(entries, entriesByType, changedOwnedOwners, handlerProvider, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return result;
        }

        private async ValueTask<InterceptionResult<int>> SavingChangesCoreAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken)
        {
            var context = eventData.Context!;
            var accessor = ResolveAccessor(context);
            if (accessor == null)
            {
                return result;
            }

            if (accessor.ConsumeRootCycleSave())
            {
                return result;
            }

            if (accessor.IsOperationActive)
            {
                // Nested SaveChanges from a lifecycle handler: suppress SQL and record a re-entry request.
                accessor.RequestReentry(HasChangedEntries(context));
                accessor.MarkSuppressedSave();
                _logger.LogDebug(
                    "Suppressed nested SaveChanges on {ContextType}; the root save operation will re-scan changes",
                    context.GetType().Name);
                return InterceptionResult<int>.SuppressWithResult(0);
            }

            var entries = GetChangedEntities(context);
            var entriesByType = SaveOperationEntityHelper.BuildEntriesByType(context);
            var changedOwnedOwners = SaveOperationEntityHelper.BuildChangedOwnedOwners(context, entriesByType);
            var snapshots = entries
                .Select(e => new EntityStateSnapshot(e, ResolvePreSaveState(e, entriesByType, changedOwnedOwners)))
                .ToArray();

            if (snapshots.Length == 0)
            {
                return result;
            }

            var handlerProvider = ResolveHandlerProvider();
            if (handlerProvider == null)
            {
                return result;
            }

            if (!accessor.TryBeginRoot(handlerProvider, snapshots))
            {
                return result;
            }

            _logger.LogDebug(
                "Started root save operation for {ContextType} with {Count} changed entities",
                context.GetType().Name, snapshots.Length);

            await RunPreSaveCycleAsync(entries, entriesByType, changedOwnedOwners, handlerProvider, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private IEFCoreWriteCoordinator RequireCoordinator(DbContext context)
        {
            // Permissive policy: a write without an ambient writable UoW passes through
            // unguarded (native EF semantics). Guards/binding apply only inside a UoW.
            var coordinator = MiCakeInterceptorPipeline.ResolveCoordinator(_ambientAccessor);
            if (coordinator != null)
            {
                return coordinator;
            }

            var noActiveUow = MiCakeInterceptorPipeline.ResolveCurrentUowServiceProvider(_ambientAccessor) == null;
            throw MiCakeInterceptorPipeline.CreateUnavailableException(context, noActiveUow);
        }

        /// <summary>
        /// Resolves the provider used for lifecycle handlers: the provider of the scope
        /// that owns the ambient unit of work. Under AddDbContextPool the provider captured
        /// at options-build time is the pool root rather than the request scope, so the
        /// owning unit of work's provider is authoritative. Without an ambient unit of work
        /// there is no owning scope, so no handlers are resolved.
        /// </summary>
        private IServiceProvider? ResolveHandlerProvider()
            => MiCakeInterceptorPipeline.ResolveCurrentUowServiceProvider(_ambientAccessor);

        private void MarkUnitOfWorkRollbackOnly(IServiceProvider handlerProvider, DbContext context)
        {
            try
            {
                var uow = handlerProvider.GetService<IUnitOfWorkManager>()?.Current;
                if (uow is IUnitOfWorkInternal internalUow)
                {
                    internalUow.MarkRollbackOnly();
                    _logger.LogDebug(
                        "Marked unit of work {UowId} rollback-only after a save re-entry failure on {ContextType}",
                        uow!.Id, context.GetType().Name);
                }
            }
            catch (Exception markEx)
            {
                _logger.LogError(markEx, "Failed to mark the unit of work rollback-only for {ContextType}",
                    context.GetType().Name);
            }
        }

        private static SaveOperationStateAccessor? ResolveAccessor(DbContext? context)
        {
            if (context == null)
            {
                return null;
            }

            try
            {
                return context.GetInfrastructure().GetService<SaveOperationStateAccessor>();
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        private int ResolveMaxSaveCycles(DbContext context)
        {
            // The global MiCakeEFCoreOptions (configured through EFCoreConfig) is authoritative;
            // the per-context extension is the fallback for options installed without module options.
            var frameProvider = _ambientAccessor.CurrentServiceProvider;
            var global = frameProvider?.GetService<IObjectAccessor<MiCakeEFCoreOptions>>()?.Value?.MaxSaveCycles;
            if (global.HasValue)
            {
                return global.Value;
            }

            var options = context.GetInfrastructure().GetService<IDbContextOptions>();
            var extension = options?.FindExtension<MiCakeSaveOperationOptionsExtension>();
            return extension?.MaxSaveCycles ?? 16;
        }

        private static bool HasChangedEntries(DbContext context)
            => SaveOperationEntityHelper.HasChangedEntries(context);

        private static List<EntityEntry> GetChangedEntities(DbContext dbContext)
            => SaveOperationEntityHelper.GetChangedEntities(dbContext);

        private static RepositoryEntityStates ResolvePreSaveState(
            EntityEntry entry,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType,
            IReadOnlySet<object> changedOwnedOwners)
            => SaveOperationEntityHelper.ResolvePreSaveState(entry, entriesByType, changedOwnedOwners);

        private static async Task RunPreSaveCycleAsync(
            IReadOnlyList<EntityEntry> entries,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType,
            IReadOnlySet<object> changedOwnedOwners,
            IServiceProvider provider,
            CancellationToken cancellationToken)
        {
            var handlers = provider.GetServices<IRepositoryPreSaveChanges>()
                .OrderBy(h => h.Order)
                .ToList();

            if (handlers.Count == 0)
            {
                return;
            }

            var stateChanges = new List<(EntityEntry Entry, EntityState NewState)>(capacity: Math.Max(1, entries.Count / 10));

            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var entry in entries)
                {
                    var originalEFState = entry.State;
                    var state = ResolvePreSaveState(entry, entriesByType, changedOwnedOwners);

                    state = await handler.PreSaveChangesAsync(state, entry.Entity, cancellationToken).ConfigureAwait(false);

                    var newEFState = state.ToEFState();
                    if (newEFState != originalEFState)
                    {
                        stateChanges.Add((entry, newEFState));
                    }
                }
            }

            for (int i = 0; i < stateChanges.Count; i++)
            {
                var (entry, newState) = stateChanges[i];
                entry.State = newState;
            }
        }

        private static async Task RunPostSaveAsync(
            IReadOnlyList<EntityStateSnapshot> snapshots,
            IServiceProvider provider,
            CancellationToken cancellationToken)
        {
            var handlers = provider.GetServices<IRepositoryPostSaveChanges>()
                .OrderBy(h => h.Order)
                .ToList();

            if (handlers.Count == 0)
            {
                return;
            }

            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < snapshots.Count; i++)
                {
                    var snapshot = snapshots[i];
                    await handler.PostSaveChangesAsync(snapshot.State, snapshot.Entry.Entity, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
