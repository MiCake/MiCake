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
            var ownerEntitiesNeedingAudit = new HashSet<object>(ReferenceEqualityComparer.Instance);

            foreach (var entry in allEntries.Where(IsEntityChanged))
            {
                changedEntries.Add(entry);
                CollectOwnerEntityIfNeeded(entry, ownerEntitiesNeedingAudit, entriesByType);
            }

            changedEntries.AddRange(ownerEntitiesNeedingAudit.Select(entity => dbContext.Entry(entity)));
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
            var entriesByType = BuildEntriesByType(entry.Context);
            return ResolvePreSaveState(entry, entriesByType, BuildChangedOwnedOwners(entry.Context, entriesByType));
        }

        public static RepositoryEntityStates ResolvePreSaveState(
            EntityEntry entry,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType)
            => ResolvePreSaveState(entry, entriesByType, BuildChangedOwnedOwners(entry.Context, entriesByType));

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

        /// <summary>
        /// Builds the set of owner entity instances (reference equality) that have at least
        /// one changed owned entry. Callers resolving many entries in one save pass build
        /// the set once and reuse it, avoiding a full change-tracker scan per entry.
        /// Reference equality prevents entities overriding <c>Equals</c> from collapsing
        /// distinct owner instances.
        /// </summary>
        public static IReadOnlySet<object> BuildChangedOwnedOwners(
            DbContext dbContext,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            var owners = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var candidate in dbContext.ChangeTracker.Entries())
            {
                if (candidate.Metadata.IsOwned() && IsEntityChanged(candidate))
                {
                    var owner = FindOwnerEntry(candidate, entriesByType);
                    if (owner != null)
                    {
                        owners.Add(owner.Entity);
                    }
                }
            }

            return owners;
        }

        /// <summary>
        /// Builds the per-type entry lookup used by the pre-save state resolver.
        /// Callers resolving many entries in one save pass build the lookup once and reuse it.
        /// </summary>
        public static IReadOnlyDictionary<Type, List<EntityEntry>> BuildEntriesByType(DbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            return dbContext.ChangeTracker.Entries()
                .GroupBy(e => e.Metadata.ClrType)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private static void CollectOwnerEntityIfNeeded(
            EntityEntry entry,
            HashSet<object> ownerEntitiesNeedingAudit,
            IReadOnlyDictionary<Type, List<EntityEntry>> entriesByType)
        {
            if (!entry.Metadata.IsOwned())
            {
                return;
            }

            var ownerEntry = FindOwnerEntry(entry, entriesByType);
            if (ownerEntry != null &&
                ownerEntry.State == EntityState.Unchanged &&
                ownerEntitiesNeedingAudit.Add(ownerEntry.Entity))
            {
                // De-duplicated by entity instance; added back as a tracker entry by the caller.
            }
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
