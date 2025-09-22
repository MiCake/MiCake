using MiCake.DDD.Extensions.Lifetime;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    internal class DefaultEFSaveChangesLifetime : IEFSaveChangesLifetime
    {
        private readonly IReadOnlyList<IRepositoryPreSaveChanges> _repositoryPreSaveChanges;
        private readonly IReadOnlyList<IRepositoryPostSaveChanges> _repositoryPostSaveChanges;

        public DefaultEFSaveChangesLifetime(
            IEnumerable<IRepositoryPreSaveChanges> repositoryPreSaveChanges,
            IEnumerable<IRepositoryPostSaveChanges> repositoryPostSaveChanges)
        {
            // Convert to readonly lists and sort once for better performance
            _repositoryPreSaveChanges = repositoryPreSaveChanges.OrderBy(p => p.Order).ToList();
            _repositoryPostSaveChanges = repositoryPostSaveChanges.OrderBy(p => p.Order).ToList();
        }

        public async Task AfterSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            // Early exit if no handlers
            if (_repositoryPostSaveChanges.Count == 0)
                return;

            // Convert to list once to avoid multiple enumeration
            var entries = entityEntries as IReadOnlyList<EntityEntry> ?? entityEntries.ToList();
            
            // Early exit if no entities
            if (entries.Count == 0)
                return;

            // Process handlers in priority order
            foreach (var postSaveChange in _repositoryPostSaveChanges)
            {
                // Check cancellation token periodically for responsiveness
                cancellationToken.ThrowIfCancellationRequested();
                
                // Batch process entities for better performance
                await ProcessEntitiesForHandler(entries, postSaveChange, cancellationToken);
            }
        }

        public async Task BeforeSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            // Early exit if no handlers
            if (_repositoryPreSaveChanges.Count == 0)
                return;

            // Convert to list once to avoid multiple enumeration
            var entries = entityEntries as IReadOnlyList<EntityEntry> ?? entityEntries.ToList();
            
            // Early exit if no entities
            if (entries.Count == 0)
                return;

            // Process handlers in priority order
            foreach (var preSaveChange in _repositoryPreSaveChanges)
            {
                // Check cancellation token periodically for responsiveness
                cancellationToken.ThrowIfCancellationRequested();
                
                // Process each entity for this handler
                foreach (var entity in entries)
                {
                    var originalEFState = entity.State;
                    var state = entity.State.ToRepositoryState();
                    
                    state = await preSaveChange.PreSaveChangesAsync(state, entity.Entity, cancellationToken);
                    
                    // Only update state if it changed to avoid unnecessary operations
                    if (state.ToEFState() != originalEFState)
                    {
                        entity.State = state.ToEFState();
                    }
                }
            }
        }

        /// <summary>
        /// Process all entities for a specific post-save handler.
        /// This method can be overridden for custom batching strategies.
        /// </summary>
        /// <param name="entries">The entity entries to process</param>
        /// <param name="handler">The post-save change handler</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task ProcessEntitiesForHandler(
            IReadOnlyList<EntityEntry> entries, 
            IRepositoryPostSaveChanges handler, 
            CancellationToken cancellationToken)
        {
            foreach (var entity in entries)
            {
                var state = entity.State.ToRepositoryState();
                await handler.PostSaveChangesAsync(state, entity.Entity, cancellationToken);
            }
        }
    }
}
