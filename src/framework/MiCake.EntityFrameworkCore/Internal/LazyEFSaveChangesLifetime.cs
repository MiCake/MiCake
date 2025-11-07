using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Lifetime;
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
                cancellationToken);
        }

        /// <summary>
        /// Common method to execute lifetime logic within a proper service scope.
        /// </summary>
        private async Task ExecuteWithScopeAsync(
            IEnumerable<EntityEntry> entityEntries,
            Func<IReadOnlyList<EntityEntry>, IServiceProvider, CancellationToken, Task> processor,
            CancellationToken cancellationToken)
        {
            // Convert to list once to avoid multiple enumeration
            var entries = entityEntries as IReadOnlyList<EntityEntry> ?? [.. entityEntries];
            
            if (entries.Count == 0)
                return;

            // Create a scope to safely resolve scoped services
            using var scope = _serviceScopeFactory.CreateScope();
            
            await processor(entries, scope.ServiceProvider, cancellationToken);
            // The scope will be disposed here, properly cleaning up scoped services
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

            var entityStates = new (EntityEntry entry, object entity, RepositoryEntityState state)[entries.Count];
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
        /// </summary>
        private static async Task ProcessPreSaveHandlersAsync(
            IReadOnlyList<EntityEntry> entries,
            IReadOnlyList<IRepositoryPreSaveChanges> handlers,
            CancellationToken cancellationToken)
        {
            // Early exit if no handlers
            if (handlers.Count == 0)
                return;

            var stateChanges = new List<(EntityEntry entry, EntityState newState)>(capacity: Math.Max(1, entries.Count / 10));

            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Process each entity for this handler
                foreach (var entry in entries)
                {
                    var originalEFState = entry.State;
                    var state = originalEFState.ToRepositoryState();
                    
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
    }
}