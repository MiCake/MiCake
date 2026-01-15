using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// EF Core interceptor for MiCake repository lifecycle events.
    /// Optimized for performance by caching changed entities and minimizing repeated scanning.
    /// Uses singleton pattern with lazy scoped service resolution.
    /// </summary>
    internal class MiCakeEFCoreInterceptor : ISaveChangesInterceptor
    {
        private readonly IEFSaveChangesLifetime _saveChangesLifetime;
        private readonly ILogger<MiCakeEFCoreInterceptor> _logger;

        private IReadOnlyList<EntityEntry> _changedEntries = [];

        /// <summary>
        /// Constructor that takes IEFSaveChangesLifetime service.
        /// The service is registered as Singleton with lazy scoped service resolution.
        /// </summary>
        /// <param name="saveChangesLifetime">The save changes lifetime service</param>
        /// <param name="logger">Logger for diagnostics</param>
        public MiCakeEFCoreInterceptor(
            IEFSaveChangesLifetime saveChangesLifetime,
            ILogger<MiCakeEFCoreInterceptor> logger)
        {
            _saveChangesLifetime = saveChangesLifetime ?? throw new ArgumentNullException(nameof(saveChangesLifetime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _changedEntries = [];
        }

        public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _changedEntries = [];
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
            try
            {
                if (_saveChangesLifetime != null && _changedEntries.Count > 0)
                {
                    await _saveChangesLifetime.AfterSaveChangesAsync(_changedEntries, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AfterSaveChangesAsync for {ContextType}",
                    eventData.Context?.GetType().Name);
                throw;
            }
            finally
            {
                _changedEntries = [];
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
                return SavingChangesAsync(eventData, result, default)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in synchronous SavingChanges for {ContextType}",
                    eventData.Context?.GetType().Name);
                throw;
            }
        }

        public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null || _saveChangesLifetime == null)
                return result;

            try
            {
                _changedEntries = GetChangedEntities(eventData.Context);

                _logger.LogDebug("SavingChangesAsync called with {Count} changed entities in {ContextType}",
                    _changedEntries.Count, eventData.Context.GetType().Name);

                if (_changedEntries.Count > 0)
                {
                    await _saveChangesLifetime.BeforeSaveChangesAsync(_changedEntries, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BeforeSaveChangesAsync for {ContextType}",
                    eventData.Context.GetType().Name);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Gets only entities that have been changed (Added, Modified, Deleted).
        /// </summary>
        private static List<EntityEntry> GetChangedEntities(DbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            var changeTracker = dbContext.ChangeTracker;
            if (!changeTracker.AutoDetectChangesEnabled && !changeTracker.HasChanges())
            {
                return [];
            }

            var allEntries = changeTracker.Entries();
            var changedEntries = new List<EntityEntry>(capacity: 16);

            foreach (var entry in allEntries)
            {
                var state = entry.State;

                if (state == EntityState.Added || state == EntityState.Modified || state == EntityState.Deleted)
                {
                    changedEntries.Add(entry);
                }
            }

            // Trim excess if we over-allocated significantly
            if (changedEntries.Capacity > changedEntries.Count * 4 && changedEntries.Count > 100)
            {
                changedEntries.TrimExcess();
            }

            return changedEntries;
        }
    }
}
