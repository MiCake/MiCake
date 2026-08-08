using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MiCake.DDD.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Result of a single-pass change-tracker scan: changed entries, the per-type entry
    /// lookup, and the set of owners with changed owned entries.
    /// </summary>
    internal sealed class SaveScanResult
    {
        public SaveScanResult(
            List<EntityEntry> changedEntries,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType,
            IReadOnlySet<object> changedOwnedOwners)
        {
            ChangedEntries = changedEntries;
            EntriesByType = entriesByType;
            ChangedOwnedOwners = changedOwnedOwners;
        }

        /// <summary>
        /// Shared immutable empty result returned when the tracker has no changes.
        /// Consumers must not mutate the contained collections.
        /// </summary>
        public static SaveScanResult Empty { get; } = new(
            [],
            new Dictionary<Type, List<EntityEntry>>(),
            new HashSet<object>(ReferenceEqualityComparer.Instance));

        /// <summary>Entities in Added/Modified/Deleted state plus unchanged owners needing audit.</summary>
        public IReadOnlyList<EntityEntry> ChangedEntries { get; }

        /// <summary>All tracker entries grouped by CLR type (including unchanged entries).</summary>
        public IReadOnlyDictionary<Type, List<EntityEntry>> EntriesByType { get; }

        /// <summary>Owner instances (reference equality) that have at least one changed owned entry.</summary>
        public IReadOnlySet<object> ChangedOwnedOwners { get; }
    }

    /// <summary>
    /// Shared changed-entity detection for the save-operation pipeline. Stateless; no
    /// static or singleton mutable caches are kept so concurrent DbContexts never share
    /// operation state.
    /// </summary>
    internal static class SaveOperationEntityHelper
    {
        /// <summary>
        /// Single-pass scan of the change tracker. Returns the changed entities
        /// (Added, Modified, Deleted) plus unchanged owner entities of changed owned
        /// entries (OwnsOne/OwnsMany) so audit timestamps are applied when value objects
        /// change, together with the per-type entry lookup and the changed-owned-owner set
        /// used by the pre-save state resolver.
        /// Cost: one main traversal over the tracker plus a pass over changed owned
        /// entries only.
        /// </summary>
        public static SaveScanResult ScanChangedEntities(DbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            var changeTracker = dbContext.ChangeTracker;
            if (!changeTracker.AutoDetectChangesEnabled && !changeTracker.HasChanges())
            {
                return SaveScanResult.Empty;
            }

            // First pass: group entries by type and collect changed entries
            // (including changed owned entries for owner resolution).
            var (entriesByType, changedEntries, changedOwnedEntries) = CollectChangedEntries(changeTracker);

            // Second pass: over changed owned entries only (a small subset), resolve each
            // owner once and feed both the audit-owner set and the changed-owner set.
            var (ownerEntitiesNeedingAudit, changedOwnedOwners) = ResolveChangedOwnedOwners(changedOwnedEntries, entriesByType);

            changedEntries.AddRange(ownerEntitiesNeedingAudit.Select(entity => dbContext.Entry(entity)));
            return new SaveScanResult(changedEntries, entriesByType, changedOwnedOwners);
        }

        /// <summary>
        /// First pass: groups all tracker entries by CLR type and collects the changed
        /// entries (including changed owned entries for owner resolution).
        /// </summary>
        private static (Dictionary<Type, List<EntityEntry>> EntriesByType, List<EntityEntry> ChangedEntries, List<EntityEntry> ChangedOwnedEntries) CollectChangedEntries(
            ChangeTracker changeTracker)
        {
            var entriesByType = new Dictionary<Type, List<EntityEntry>>();
            var changedEntries = new List<EntityEntry>(capacity: 16);
            var changedOwnedEntries = new List<EntityEntry>(capacity: 16);

            foreach (var entry in changeTracker.Entries())
            {
                if (entriesByType.TryGetValue(entry.Metadata.ClrType, out var typeEntries))
                {
                    typeEntries.Add(entry);
                }
                else
                {
                    entriesByType[entry.Metadata.ClrType] = [entry];
                }

                if (IsEntityChanged(entry))
                {
                    changedEntries.Add(entry);
                    if (entry.Metadata.IsOwned())
                    {
                        changedOwnedEntries.Add(entry);
                    }
                }
            }

            return (entriesByType, changedEntries, changedOwnedEntries);
        }

        /// <summary>
        /// Second pass: resolves the owner of each changed owned entry once and feeds both
        /// the audit-owner set and the changed-owner set.
        /// </summary>
        private static (HashSet<object> OwnerEntitiesNeedingAudit, HashSet<object> ChangedOwnedOwners) ResolveChangedOwnedOwners(
            List<EntityEntry> changedOwnedEntries,
            Dictionary<Type, List<EntityEntry>> entriesByType)
        {
            var ownerEntitiesNeedingAudit = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var changedOwnedOwners = new HashSet<object>(ReferenceEqualityComparer.Instance);

            for (int i = 0; i < changedOwnedEntries.Count; i++)
            {
                var ownedEntry = changedOwnedEntries[i];
                var ownerEntry = FindOwnerEntry(ownedEntry, entriesByType);
                if (ownerEntry == null)
                {
                    continue;
                }

                if (ownerEntry.State == EntityState.Unchanged)
                {
                    ownerEntitiesNeedingAudit.Add(ownerEntry.Entity);
                }

                changedOwnedOwners.Add(ownerEntry.Entity);
            }

            return (ownerEntitiesNeedingAudit, changedOwnedOwners);
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

        public static RepositoryEntityStates ResolvePreSaveState(
            EntityEntry entry,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType,
            IReadOnlySet<object> changedOwnedOwners)
        {
            var state = entry.State.ToRepositoryState();
            if (entry.State == EntityState.Unchanged && changedOwnedOwners.Contains(entry.Entity))
            {
                return RepositoryEntityStates.Modified;
            }

            return state;
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
