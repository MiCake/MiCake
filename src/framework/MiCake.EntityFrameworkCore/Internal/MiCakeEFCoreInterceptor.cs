using MiCake.DDD.Extensions.Lifetime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// This interceptor is designed to execute the <see cref="IRepositoryLifetime"/> events.
    /// Optimized for performance with large datasets and uses singleton IEFSaveChangesLifetime
    /// which handles scoped service resolution internally.
    /// </summary>
    internal class MiCakeEFCoreInterceptor : ISaveChangesInterceptor
    {
        private readonly IEFSaveChangesLifetime _saveChangesLifetime;

        // Cache for changed entities to avoid repeated scanning
        private IReadOnlyList<EntityEntry> _changedEntries = [];

        /// <summary>
        /// Constructor that takes IEFSaveChangesLifetime service.
        /// The service is now registered as Singleton with lazy scoped service resolution.
        /// </summary>
        /// <param name="saveChangesLifetime">The save changes lifetime service</param>
        public MiCakeEFCoreInterceptor(IEFSaveChangesLifetime saveChangesLifetime)
        {
            _saveChangesLifetime = saveChangesLifetime ?? throw new ArgumentNullException(nameof(saveChangesLifetime));
        }
        
        public void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            // Clear cache on failure
            _changedEntries = [];
        }

        public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            // Clear cache on failure
            _changedEntries = [];
            return Task.CompletedTask;
        }

        public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            //be careful ,this will risks a deadlock.
            //when save data in aspnet core ,shuold use DbContext.SaveChangesAsync().
            SavedChangesAsync(eventData, result).GetAwaiter().GetResult();
            return result;
        }

        public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_saveChangesLifetime != null && _changedEntries.Count > 0)
                {
                    await _saveChangesLifetime.AfterSaveChangesAsync(_changedEntries, cancellationToken);
                }
            }
            finally
            {
                // Clear cache after processing
                _changedEntries = [];
            }
            return result;
        }

        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            //be careful ,this will risks a deadlock.
            //when save data in aspnet core ,shuold use DbContext.SaveChangesAsync().
            SavingChangesAsync(eventData, result).GetAwaiter().GetResult();
            return result;
        }

        public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null && _saveChangesLifetime != null)
            {
                // Get only changed entities for performance
                _changedEntries = GetChangedEntities(eventData.Context);
                
                if (_changedEntries.Count > 0)
                {
                    await _saveChangesLifetime.BeforeSaveChangesAsync(_changedEntries, cancellationToken);
                }
            }
            return result;
        }

        /// <summary>
        /// Get only entities that have been changed (Added, Modified, Deleted).
        /// This is much more performant than getting all tracked entities.
        /// </summary>
        /// <param name="dbContext">The DbContext instance</param>
        /// <returns>Read-only list of changed entity entries</returns>
        private static IReadOnlyList<EntityEntry> GetChangedEntities(DbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            // Only get entities that have actually changed - this is the key performance optimization
            var changedEntries = dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || 
                           e.State == EntityState.Modified || 
                           e.State == EntityState.Deleted)
                .ToList();
                
            return changedEntries.AsReadOnly();
        }
    }
}
