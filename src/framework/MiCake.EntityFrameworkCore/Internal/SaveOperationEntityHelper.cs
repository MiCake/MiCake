using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MiCake.DDD.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Shared changed-entity detection for the save-operation pipeline. Stateless; no
    /// static or singleton mutable caches are kept so concurrent DbContexts never share
    /// operation state.
    /// </summary>
    internal static class SaveOperationEntityHelper
    {
        /// <summary>
        /// Gets only entities that have been changed (Added, Modified, Deleted).
        /// Also includes owner entities of changed owned entities to properly handle
        /// audit timestamps when value objects change via OwnsOne/OwnsMany.
        /// </summary>
        public static List<EntityEntry> GetChangedEntities(DbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            var changeTracker = dbContext.ChangeTracker;
            if (!changeTracker.AutoDetectChangesEnabled && !changeTracker.HasChanges())
            {
                return [];
            }

            var allEntries = changeTracker.Entries().ToList();
            var entriesByType = allEntries
                .GroupBy(e => e.Metadata.ClrType)
                .ToDictionary(g => g.Key, g => g.ToList());

            var changedEntries = new List<EntityEntry>(capacity: 16);
            var ownerEntriesNeedingAudit = new HashSet<EntityEntry>();

            foreach (var entry in allEntries.Where(IsEntityChanged))
            {
                changedEntries.Add(entry);
                CollectOwnerEntityIfNeeded(entry, ownerEntriesNeedingAudit, entriesByType);
            }

            changedEntries.AddRange(ownerEntriesNeedingAudit);
            return changedEntries;
        }

        public static bool HasChangedEntries(DbContext context)
        {
            var changeTracker = context.ChangeTracker;
            if (!changeTracker.AutoDetectChangesEnabled && !changeTracker.HasChanges())
            {
                return false;
            }

            return changeTracker.Entries().Any(IsEntityChanged);
        }

        public static bool IsEntityChanged(EntityEntry entry)
        {
            var state = entry.State;
            return state == EntityState.Added || state == EntityState.Modified || state == EntityState.Deleted;
        }

        public static RepositoryEntityStates ResolvePreSaveState(EntityEntry entry)
        {
            var state = entry.State.ToRepositoryState();
            if (entry.State == EntityState.Unchanged && HasOwnedEntityChanges(entry))
            {
                return RepositoryEntityStates.Modified;
            }

            return state;
        }

        private static void CollectOwnerEntityIfNeeded(
            EntityEntry entry,
            HashSet<EntityEntry> ownerEntriesNeedingAudit,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType)
        {
            if (!entry.Metadata.IsOwned())
            {
                return;
            }

            var ownerEntry = FindOwnerEntry(entry, entriesByType);
            if (ownerEntry != null &&
                ownerEntry.State == EntityState.Unchanged &&
                !ownerEntriesNeedingAudit.Contains(ownerEntry))
            {
                ownerEntriesNeedingAudit.Add(ownerEntry);
            }
        }

        private static bool HasOwnedEntityChanges(EntityEntry entry)
        {
            // An owned change surfaces as an owned entry in the change tracker whose owner
            // entry is this entry; only unchanged entries reach this path, so the scan is
            // bounded to owned candidates of the same owner type.
            var entriesByType = entry.Context.ChangeTracker.Entries()
                .GroupBy(e => e.Metadata.ClrType)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var candidate in entry.Context.ChangeTracker.Entries())
            {
                if (candidate.Metadata.IsOwned() &&
                    IsEntityChanged(candidate) &&
                    ReferenceEquals(FindOwnerEntry(candidate, entriesByType), entry))
                {
                    return true;
                }
            }

            return false;
        }

        private static EntityEntry? FindOwnerEntry(
            EntityEntry ownedEntry,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType)
        {
            var ownership = ownedEntry.Metadata.FindOwnership();
            if (ownership == null)
            {
                return null;
            }

            var principalKey = ownership.PrincipalKey;
            var foreignKeyProperties = ownership.Properties;

            if (principalKey.Properties.Count != foreignKeyProperties.Count)
            {
                return null;
            }

            var keyValues = new object?[principalKey.Properties.Count];
            for (int i = 0; i < foreignKeyProperties.Count; i++)
            {
                keyValues[i] = ownedEntry.Property(foreignKeyProperties[i].Name).CurrentValue;
                if (keyValues[i] == null)
                {
                    return null;
                }
            }

            var ownerEntityType = ownership.PrincipalEntityType.ClrType;
            if (!entriesByType.TryGetValue(ownerEntityType, out var ownerCandidates))
            {
                return null;
            }

            return ownerCandidates.FirstOrDefault(e => KeyValuesMatch(e, principalKey, keyValues));
        }

        private static bool KeyValuesMatch(EntityEntry entry, Microsoft.EntityFrameworkCore.Metadata.IKey primaryKey, object?[] keyValues)
        {
            var pkProperties = primaryKey.Properties;
            if (pkProperties.Count != keyValues.Length)
            {
                return false;
            }

            for (int i = 0; i < pkProperties.Count; i++)
            {
                var currentValue = entry.Property(pkProperties[i].Name).CurrentValue;
                if (!Equals(currentValue, keyValues[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
