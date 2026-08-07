using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Stateless, scoped implementation of <see cref="IEFSaveChangesLifetime"/>.
    /// Resolves pre/post-save handlers directly from the provider of the scope that owns
    /// the unit of work; it never creates unrelated temporary scopes and holds no mutable
    /// operation state. The root save-operation state machine in
    /// <see cref="MiCakeEFCoreInterceptor"/> drives lifecycle execution; this type exists
    /// for backward compatibility with callers that use the public interface directly.
    /// </summary>
    internal class LazyEFSaveChangesLifetime : IEFSaveChangesLifetime
    {
        private readonly IServiceProvider _serviceProvider;

        public LazyEFSaveChangesLifetime(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task AfterSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            var entries = entityEntries as IReadOnlyList<EntityEntry> ?? [.. entityEntries];
            if (entries.Count == 0)
            {
                return;
            }

            var handlers = _serviceProvider.GetServices<IRepositoryPostSaveChanges>()
                .OrderBy(h => h.Order)
                .ToList();

            if (handlers.Count == 0)
            {
                return;
            }

            var entriesByType = SaveOperationEntityHelper.BuildEntriesByType(entries[0].Context);
            var changedOwnedOwners = SaveOperationEntityHelper.BuildChangedOwnedOwners(entries[0].Context, entriesByType);
            var snapshots = entries
                .Select(e => new EntityStateSnapshot(e, SaveOperationEntityHelper.ResolvePreSaveState(e, entriesByType, changedOwnedOwners)))
                .ToArray();

            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < snapshots.Length; i++)
                {
                    await handler.PostSaveChangesAsync(snapshots[i].State, snapshots[i].Entry.Entity, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task BeforeSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            var entries = entityEntries as IReadOnlyList<EntityEntry> ?? [.. entityEntries];
            if (entries.Count == 0)
            {
                return;
            }

            var handlers = _serviceProvider.GetServices<IRepositoryPreSaveChanges>()
                .OrderBy(h => h.Order)
                .ToList();

            if (handlers.Count == 0)
            {
                return;
            }

            var entriesByType = SaveOperationEntityHelper.BuildEntriesByType(entries[0].Context);
            var changedOwnedOwners = SaveOperationEntityHelper.BuildChangedOwnedOwners(entries[0].Context, entriesByType);
            var stateChanges = new List<(EntityEntry Entry, EntityState NewState)>(capacity: Math.Max(1, entries.Count / 10));

            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var entry in entries)
                {
                    var originalEFState = entry.State;
                    var state = SaveOperationEntityHelper.ResolvePreSaveState(entry, entriesByType, changedOwnedOwners);

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

        private static bool IsEntityChanged(EntityEntry entry)
            => SaveOperationEntityHelper.IsEntityChanged(entry);
    }
}
