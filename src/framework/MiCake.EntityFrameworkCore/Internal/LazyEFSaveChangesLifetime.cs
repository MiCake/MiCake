using MiCake.DDD.Extensions.Lifetime;
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
            try
            {
                // Convert to list once to avoid multiple enumeration
                var entries = entityEntries as IReadOnlyList<EntityEntry> ?? entityEntries.ToList();
                
                // Early exit if no entities
                if (entries.Count == 0)
                    return;

                // Create a scope to safely resolve scoped services
                using var scope = _serviceScopeFactory.CreateScope();
                
                await processor(entries, scope.ServiceProvider, cancellationToken);
                // The scope will be disposed here, properly cleaning up scoped services
            }
            catch (Exception ex)
            {
                // If service resolution fails, continue silently to allow graceful degradation
                // This can happen during application startup when services are not properly registered
                System.Diagnostics.Debug.WriteLine($"LazyEFSaveChangesLifetime execution failed: {ex.Message}");
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

            // Process handlers in priority order
            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Process each entity for this handler
                foreach (var entity in entries)
                {
                    var state = entity.State.ToRepositoryState();
                    await handler.PostSaveChangesAsync(state, entity.Entity, cancellationToken);
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

            // Process handlers in priority order
            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Process each entity for this handler
                foreach (var entity in entries)
                {
                    var originalEFState = entity.State;
                    var state = entity.State.ToRepositoryState();
                    
                    state = await handler.PreSaveChangesAsync(state, entity.Entity, cancellationToken);
                    
                    // Only update state if it changed to avoid unnecessary operations
                    if (state.ToEFState() != originalEFState)
                    {
                        entity.State = state.ToEFState();
                    }
                }
            }
        }
    }
}