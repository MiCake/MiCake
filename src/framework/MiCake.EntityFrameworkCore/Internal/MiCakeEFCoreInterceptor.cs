using MiCake.Util.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    /// Optimized for performance by caching changed entities and minimizing repeated scanning.
    /// Uses singleton pattern with lazy scoped service resolution.
    /// </summary>
    internal class MiCakeEFCoreInterceptor : ISaveChangesInterceptor
    {
        private readonly IEFSaveChangesLifetime _saveChangesLifetime;
        private readonly ILogger<MiCakeEFCoreInterceptor> _logger;

        private IReadOnlyList<EntityEntry> _changedEntries = [];

        // Cache for owner entity lookups to avoid repeated ChangeTracker scanning
        // Key: (OwnerType, KeyValues hash), Value: EntityEntry
        private static readonly BoundedLruCache<int, EntityEntry?> s_ownerLookupCache = new(maxSize: 100);

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
            s_ownerLookupCache.Clear();
        }

        public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _changedEntries = [];
            s_ownerLookupCache.Clear();
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
                s_ownerLookupCache.Clear();
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
        /// Also includes owner entities of changed owned entities to properly handle
        /// audit timestamps when value objects change via OwnsOne/OwnsMany.
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
            var ownerEntriesNeedingAudit = new HashSet<EntityEntry>();

            foreach (var entry in allEntries.Where(IsEntityChanged))
            {
                changedEntries.Add(entry);
                CollectOwnerEntityIfNeeded(entry, ownerEntriesNeedingAudit);
            }

            // Add owner entries that need audit updates (due to owned entity changes)
            changedEntries.AddRange(ownerEntriesNeedingAudit);

            TrimExcessCapacityIfNeeded(changedEntries);

            return changedEntries;
        }

        /// <summary>
        /// Checks if an entity has been changed (Added, Modified, or Deleted).
        /// </summary>
        private static bool IsEntityChanged(EntityEntry entry)
        {
            var state = entry.State;
            return state == EntityState.Added || state == EntityState.Modified || state == EntityState.Deleted;
        }

        /// <summary>
        /// Collects owner entities for owned entities that have changed.
        /// </summary>
        private static void CollectOwnerEntityIfNeeded(EntityEntry entry, HashSet<EntityEntry> ownerEntriesNeedingAudit)
        {
            if (!entry.Metadata.IsOwned())
                return;

            var ownerEntry = FindOwnerEntry(entry);
            if (ownerEntry != null && 
                ownerEntry.State == EntityState.Unchanged &&
                !ownerEntriesNeedingAudit.Contains(ownerEntry))
            {
                ownerEntriesNeedingAudit.Add(ownerEntry);
            }
        }

        /// <summary>
        /// Trims excess capacity if over-allocated significantly.
        /// </summary>
        private static void TrimExcessCapacityIfNeeded(List<EntityEntry> changedEntries)
        {
            if (changedEntries.Capacity > changedEntries.Count * 4 && changedEntries.Count > 100)
            {
                changedEntries.TrimExcess();
            }
        }

        /// <summary>
        /// Finds the owner entity entry for an owned entity.
        /// Uses caching to avoid repeated ChangeTracker scanning.
        /// </summary>
        /// <param name="ownedEntry">The owned entity entry</param>
        /// <returns>The owner entity entry, or null if not found</returns>
        private static EntityEntry? FindOwnerEntry(EntityEntry ownedEntry)
        {
            // Get the ownership relationship from metadata
            var ownership = ownedEntry.Metadata.FindOwnership();
            if (ownership == null)
                return null;

            var principalKey = ownership.PrincipalKey;
            var foreignKeyProperties = ownership.Properties;
            
            if (principalKey.Properties.Count == foreignKeyProperties.Count)
            {
                // Build the primary key values from the owned entity's foreign key
                var keyValues = new object?[principalKey.Properties.Count];
                for (int i = 0; i < foreignKeyProperties.Count; i++)
                {
                    keyValues[i] = ownedEntry.Property(foreignKeyProperties[i].Name).CurrentValue;
                    if (keyValues[i] == null)
                        return null; // Cannot find owner without complete foreign key
                }

                var ownerEntityType = ownership.PrincipalEntityType.ClrType;
                
                // Create cache key from context, owner type, and key values
                var cacheKey = keyValues.Aggregate(
                    HashCode.Combine(ownedEntry.Context.GetHashCode(), ownerEntityType.GetHashCode()),
                    (hash, val) => HashCode.Combine(hash, val));

                return s_ownerLookupCache.GetOrAdd(cacheKey, _ =>
                {
                    var dbContext = ownedEntry.Context;
                    try
                    {
                        var trackedOwner = dbContext.ChangeTracker.Entries()
                            .FirstOrDefault(e => 
                                e.Metadata.ClrType == ownerEntityType &&
                                KeyValuesMatch(e, principalKey, keyValues));
                        
                        return trackedOwner;
                    }
                    catch (Exception)
                    {
                        // Fallback: if any error occurs (e.g., disposed context), return null
                        return null;
                    }
                });
            }

            return null;
        }

        /// <summary>
        /// Helper method to check if an entity's primary key matches the given values.
        /// </summary>
        private static bool KeyValuesMatch(EntityEntry entry, Microsoft.EntityFrameworkCore.Metadata.IKey primaryKey, object?[] keyValues)
        {
            var pkProperties = primaryKey.Properties;
            if (pkProperties.Count != keyValues.Length)
                return false;

            for (int i = 0; i < pkProperties.Count; i++)
            {
                var currentValue = entry.Property(pkProperties[i].Name).CurrentValue;
                if (!Equals(currentValue, keyValues[i]))
                    return false;
            }

            return true;
        }
    }
}
