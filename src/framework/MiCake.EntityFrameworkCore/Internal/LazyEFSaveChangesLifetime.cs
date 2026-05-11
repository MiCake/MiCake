using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.Util.Cache;
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
    /// A singleton wrapper for IEFSaveChangesLifetime that safely resolves scoped services.
    /// This enables using the lifetime service in interceptors by using IServiceScopeFactory
    /// to properly create scopes when needed, respecting service lifetimes.
    /// </summary>
    internal class LazyEFSaveChangesLifetime : IEFSaveChangesLifetime
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IServiceScope? _currentScope;

        // Cache for HasOwnedEntityChanges results to avoid repeated navigation tree walks
        private static readonly AsyncLocal<BoundedLruCache<EntityEntry, bool>?> s_ownedChangesCache = new();

        public LazyEFSaveChangesLifetime(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task AfterSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            await ExecuteWithScopeAsync(
                entityEntries,
                async (entries, serviceProvider, token) =>
                {
                    var handlers = serviceProvider.GetServices<IRepositoryPostSaveChanges>()
                        .OrderBy(p => p.Order).ToList();

                    await ProcessPostSaveHandlersAsync(entries, handlers, token);
                },
                isPreSave: false,
                cancellationToken);
        }

        public async Task BeforeSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            await ExecuteWithScopeAsync(
                entityEntries,
                async (entries, serviceProvider, token) =>
                {
                    var handlers = serviceProvider.GetServices<IRepositoryPreSaveChanges>()
                        .OrderBy(p => p.Order).ToList();

                    await ProcessPreSaveHandlersAsync(entries, handlers, token);
                },
                isPreSave: true,
                cancellationToken);
        }

        /// <summary>
        /// Common method to execute lifetime logic within a proper service scope.
        /// </summary>
        private async Task ExecuteWithScopeAsync(
            IEnumerable<EntityEntry> entityEntries,
            Func<IReadOnlyList<EntityEntry>, IServiceProvider, CancellationToken, Task> processor,
            bool isPreSave,
            CancellationToken cancellationToken)
        {
            // Convert to list once to avoid multiple enumeration
            var entries = entityEntries as IReadOnlyList<EntityEntry> ?? [.. entityEntries];

            if (entries.Count == 0)
                return;

            IServiceScope scope;
            if (isPreSave)
            {
                // Create scope for Pre-save, store it for potential reuse in Post-save
                _currentScope = _serviceScopeFactory.CreateScope();
                scope = _currentScope;
            }
            else
            {
                // Use existing scope from Pre-save, or create new if none
                scope = _currentScope ?? _serviceScopeFactory.CreateScope();
            }

            try
            {
                await processor(entries, scope.ServiceProvider, cancellationToken);
            }
            finally
            {
                if (!isPreSave)
                {
                    // Dispose scope after Post-save
                    scope.Dispose();
                    _currentScope = null;
                }
            }
        }

        /// <summary>
        /// Process post-save handlers for all entities.
        /// </summary>
        private static async Task ProcessPostSaveHandlersAsync(
            IReadOnlyList<EntityEntry> entries,
            IReadOnlyList<IRepositoryPostSaveChanges> handlers,
            CancellationToken cancellationToken)
        {
            // Early exit if no handlers
            if (handlers.Count == 0)
                return;

            var entityStates = new (EntityEntry entry, object entity, RepositoryEntityStates state)[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                entityStates[i] = (entry, entry.Entity, entry.State.ToRepositoryState());
            }

            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < entityStates.Length; i++)
                {
                    var (_, entity, state) = entityStates[i];
                    await handler.PostSaveChangesAsync(state, entity, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Process pre-save handlers for all entities.
        /// Handles special case where owner entities are included due to owned entity changes.
        /// </summary>
        private static async Task ProcessPreSaveHandlersAsync(
            IReadOnlyList<EntityEntry> entries,
            IReadOnlyList<IRepositoryPreSaveChanges> handlers,
            CancellationToken cancellationToken)
        {
            // Early exit if no handlers
            if (handlers.Count == 0)
                return;

            // Initialize async-local cache if needed
            s_ownedChangesCache.Value ??= new BoundedLruCache<EntityEntry, bool>(maxSize: 50);

            try
            {
                var stateChanges = new List<(EntityEntry entry, EntityState newState)>(capacity: Math.Max(1, entries.Count / 10));

                foreach (var handler in handlers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Process each entity for this handler
                    foreach (var entry in entries)
                    {
                        var originalEFState = entry.State;
                        var state = originalEFState.ToRepositoryState();

                        // Special handling: If entity is Unchanged but included in the list,
                        // it means it's an owner entity whose owned entity changed.
                        // Treat it as Modified for audit purposes.
                        if (originalEFState == EntityState.Unchanged && HasOwnedEntityChanges(entry))
                        {
                            state = RepositoryEntityStates.Modified;
                        }

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
            finally
            {
                // Clear cache after processing to avoid stale data
                s_ownedChangesCache.Value?.Clear();
            }
        }

        /// <summary>
        /// Checks if an entity has any owned entities that have been changed.
        /// Uses caching to avoid repeated navigation tree walks for the same entity.
        /// This is used to determine if an owner entity should be treated as Modified
        /// </summary>
        /// <param name="entry">The owner entity entry</param>
        /// <returns>True if the entity has changed owned entities</returns>
        private static bool HasOwnedEntityChanges(EntityEntry entry)
        {
            // Use cache to avoid repeated checks for same entity
            s_ownedChangesCache.Value ??= new BoundedLruCache<EntityEntry, bool>(maxSize: 50);

            return s_ownedChangesCache.Value.GetOrAdd(entry, static e =>
            {
                // Check all navigations to find owned entities
                return e.Navigations.Any(HasChangedOwnedEntity);
            });
        }

        /// <summary>
        /// Checks if a navigation property points to a changed owned entity.
        /// </summary>
        private static bool HasChangedOwnedEntity(NavigationEntry navigation)
        {
            var navigationMetadata = navigation.Metadata;
            
            // Check if this navigation points to an owned type
            if (navigationMetadata.TargetEntityType?.IsOwned() != true)
                return false;

            // For reference navigations (OwnsOne)
            if (navigation is ReferenceEntry referenceEntry)
            {
                return IsOwnedReferenceChanged(referenceEntry);
            }
            
            // For collection navigations (OwnsMany)
            if (navigation is CollectionEntry collectionEntry && collectionEntry.IsModified)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if an owned reference navigation has changed.
        /// </summary>
        private static bool IsOwnedReferenceChanged(ReferenceEntry referenceEntry)
        {
            var targetEntry = referenceEntry.TargetEntry;
            if (targetEntry == null)
                return false;

            return targetEntry.State == EntityState.Added || 
                   targetEntry.State == EntityState.Modified || 
                   targetEntry.State == EntityState.Deleted;
        }
    }
}