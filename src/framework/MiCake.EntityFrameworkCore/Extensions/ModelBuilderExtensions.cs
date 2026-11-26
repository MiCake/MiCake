using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiCake.DDD.Infrastructure.Store;
using MiCake.Util.Cache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// Bounded cache for entity convention results to improve performance and prevent memory leaks
    /// </summary>
    internal static class EntityConventionCache
    {
        // Cache size constants for convention caching performance tuning
        private const int ConventionEnginesCacheSize = 50;
        private const int EntityContextsCacheSize = 2000;
        private const int PropertyContextsCacheSize = 5000;

        // Bounded caches to prevent unbounded memory growth in long-running applications
        private static readonly BoundedLruCache<string, StoreConventionEngine> _conventionEngines = new(maxSize: ConventionEnginesCacheSize);
        private static readonly BoundedLruCache<(string engineKey, Type entityType), EntityConventionContext> _entityContexts = new(maxSize: EntityContextsCacheSize);
        private static readonly BoundedLruCache<(string engineKey, Type entityType, string propertyName), PropertyConventionContext> _propertyContexts = new(maxSize: PropertyContextsCacheSize);

        public static StoreConventionEngine GetOrCreateConventionEngine(string engineKey, Func<StoreConventionEngine> factory)
        {
            return _conventionEngines.GetOrAdd(engineKey, _ => factory());
        }

        public static EntityConventionContext GetOrCreateEntityContext(string engineKey, Type entityType, Func<EntityConventionContext> factory)
        {
            return _entityContexts.GetOrAdd((engineKey, entityType), _ => factory());
        }

        public static PropertyConventionContext GetOrCreatePropertyContext(string engineKey, Type entityType, string propertyName, Func<PropertyConventionContext> factory)
        {
            return _propertyContexts.GetOrAdd((engineKey, entityType, propertyName), _ => factory());
        }

        public static void Clear()
        {
            _conventionEngines.Clear();
            _entityContexts.Clear();
            _propertyContexts.Clear();
        }

        // Internal method for testing only - do not use in production
        internal static void ClearForTesting()
        {
            Clear();
        }
    }

    /// <summary>
    /// Provider for getting the configured StoreConventionEngine
    /// </summary>
    internal static class MiCakeConventionEngineProvider
    {
        internal const string LoggerCategory = "MiCake.EntityFrameworkCore.Conventions";

        private static volatile StoreConventionEngine? _conventionEngine;
        private static ILogger? _logger;

        internal static void SetConventionEngine(StoreConventionEngine engine)
        {
            _conventionEngine = engine;
        }

        internal static StoreConventionEngine? GetConventionEngine()
        {
            return _conventionEngine;
        }

        internal static void SetLogger(ILogger logger)
        {
            Volatile.Write(ref _logger, logger);
        }

        internal static ILogger GetLogger()
        {
            return Volatile.Read(ref _logger) ?? NullLogger.Instance;
        }

        /// <summary>
        /// Clears the stored logger only when it is still the given instance,
        /// so a logger stored by a newer initialization is left untouched.
        /// </summary>
        internal static void ClearLogger(ILogger expected)
        {
            Interlocked.CompareExchange(ref _logger, null, expected);
        }

        internal static void Clear()
        {
            _conventionEngine = null;
            Volatile.Write(ref _logger, null);
        }
    }

    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Apply MiCake store conventions to the model using the configured convention engine
        /// </summary>
        public static void ApplyMiCakeConventions(this ModelBuilder modelBuilder, params Type[] entityTypes)
        {
            ApplyConventions(modelBuilder, entityTypes, null);
        }

        internal static void ApplyConventions(ModelBuilder modelBuilder, Type[] entityTypes, ILogger? logger)
        {
            if (entityTypes == null || entityTypes.Length == 0)
                return;

            logger ??= MiCakeConventionEngineProvider.GetLogger();

            // Get the configured convention engine from MiCake container
            var engine = MiCakeConventionEngineProvider.GetConventionEngine();
            if (engine == null)
            {
                logger.Log(
                    EF.IsDesignTime ? LogLevel.Debug : LogLevel.Warning,
                    "No MiCake convention engine is configured; conventions, including the soft-delete query filter, were not applied to {EntityTypeCount} entity types",
                    entityTypes.Length);
                return;
            }

            ApplyConventionsInternal(modelBuilder, entityTypes, engine, logger);
        }

        private static void ApplyConventionsInternal(ModelBuilder modelBuilder, Type[] entityTypes, StoreConventionEngine engine, ILogger logger)
        {
            var engineKey = engine.GetHashCode().ToString();

            var ownedTypes = modelBuilder.Model.GetEntityTypes()
                .Where(t => t.IsOwned())
                .Select(t => t.ClrType)
                .ToHashSet();

            foreach (var entityType in entityTypes)
            {
                try
                {
                    // First check if any convention can apply to this entity type using CanApply
                    if (!CanAnyConventionApply(engine, entityType))
                        continue;

                    if (ownedTypes.Contains(entityType))
                    {
                        logger.LogDebug("Skipping conventions for {EntityType}: configured as owned or complex type", entityType.Name);
                        continue;
                    }

                    ApplyEntityLevelConventions(modelBuilder, entityType, engine, engineKey, logger);
                    ApplyPropertyLevelConventions(modelBuilder, entityType, engine, engineKey, logger);
                }
                catch (MiCakeConventionException)
                {
                    throw;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("cannot be configured as an entity"))
                {
                    // Defensive: owned types are skipped above; this covers EF reporting another owned or complex configuration conflict
                    logger.LogDebug("Skipping conventions for {EntityType}: configured as owned or complex type", entityType.Name);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to apply conventions for {EntityType}", entityType.Name);
                }
            }
        }

        private static bool CanAnyConventionApply(StoreConventionEngine engine, Type entityType)
        {
            // Use the engine's built-in method to check if any convention can apply
            return engine.CanApplyToEntityType(entityType);
        }

        private static void ApplyEntityLevelConventions(
            ModelBuilder modelBuilder,
            Type entityType,
            StoreConventionEngine engine,
            string engineKey,
            ILogger logger)
        {
            try
            {
                // Get cached entity context
                var entityContext = EntityConventionCache.GetOrCreateEntityContext(engineKey, entityType,
                    () => engine.ApplyEntityConventions(entityType));

                var shouldResolveEntity = entityContext.NeedApplyEntityConvention;
                if (!shouldResolveEntity)
                    return;

                EntityTypeBuilder? entityBuilder = modelBuilder.Entity(entityType);
                if (entityContext.EnableSoftDeletion && entityContext.QueryFilter != null)
                {
                    ApplySoftDeleteFilter(modelBuilder, entityBuilder, entityType, entityContext.QueryFilter, engine, engineKey, logger);
                }
                else if (entityContext.QueryFilter != null)
                {
                    logger.LogWarning(
                        "A convention produced a query filter for {EntityType} without enabling soft deletion; the filter was not applied",
                        entityType.Name);
                }

                // Apply ignored properties at entity level
                foreach (var ignoredProperty in entityContext.IgnoredProperties)
                {
                    var existingProperty = entityBuilder.Metadata.FindProperty(ignoredProperty);
                    if (existingProperty == null)
                    {
                        entityBuilder.Ignore(ignoredProperty);
                    }
                }
            }
            catch (MiCakeConventionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to apply entity-level conventions for {EntityType}", entityType.Name);
            }
        }

        private static void ApplySoftDeleteFilter(
            ModelBuilder modelBuilder,
            EntityTypeBuilder entityBuilder,
            Type entityType,
            LambdaExpression queryFilter,
            StoreConventionEngine engine,
            string engineKey,
            ILogger logger)
        {
            var metadata = entityBuilder.Metadata;

            // EF Core accepts query filters on the root of a hierarchy only; the root's filter covers derived types.
            if (metadata.BaseType != null)
            {
                var root = metadata.GetRootType();
                var rootContext = EntityConventionCache.GetOrCreateEntityContext(engineKey, root.ClrType,
                    () => engine.ApplyEntityConventions(root.ClrType));
                if (rootContext.EnableSoftDeletion && rootContext.QueryFilter != null)
                {
                    // The root may not be one of the scanned domain types, so make sure it carries the filter.
                    ApplySoftDeleteFilter(modelBuilder, modelBuilder.Entity(root.ClrType), root.ClrType, rootContext.QueryFilter, engine, engineKey, logger);
                    return;
                }

                var rootProblem = rootContext.EnableSoftDeletion
                    ? "enables soft deletion but its conventions did not produce a query filter"
                    : "is not soft-deletable";
                throw new MiCakeConventionException(
                    $"Entity type '{entityType.Name}' is soft-deletable but its hierarchy root '{root.ClrType.Name}' {rootProblem}. " +
                    "EF Core only allows query filters on the root entity type. " +
                    $"Make '{root.ClrType.Name}' soft-deletable with a query filter or remove soft deletion from '{entityType.Name}'.",
                    entityType);
            }

            var declaredFilters = metadata.GetDeclaredQueryFilters();
            if (declaredFilters.Any(filter => filter.IsAnonymous))
            {
                throw new MiCakeConventionException(
                    $"Entity type '{entityType.Name}' is soft-deletable and already has an anonymous query filter. " +
                    "EF Core does not allow anonymous and named query filters on the same entity type. " +
                    "Configure your own filters with a name, for example HasQueryFilter(\"MyApp.Tenant\", expression), " +
                    "and remove the anonymous HasQueryFilter call.",
                    entityType);
            }

            if (declaredFilters.Any(filter => filter.Key == MiCakeQueryFilterNames.SoftDelete))
            {
                logger.LogDebug("Query filter {FilterName} is already present on {EntityType}; leaving it unchanged", MiCakeQueryFilterNames.SoftDelete, entityType.Name);
                return;
            }

            entityBuilder.HasQueryFilter(MiCakeQueryFilterNames.SoftDelete, queryFilter);
        }

        private static void ApplyPropertyLevelConventions(
            ModelBuilder modelBuilder,
            Type entityType,
            StoreConventionEngine engine,
            string engineKey,
            ILogger logger)
        {
            var properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                try
                {
                    ApplyConventionsForProperty(modelBuilder, entityType, property, engine, engineKey);
                }
                catch (MiCakeConventionException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to apply property conventions for {EntityType}.{PropertyName}", entityType.Name, property.Name);
                }
            }
        }

        private static void ApplyConventionsForProperty(
            ModelBuilder modelBuilder,
            Type entityType,
            System.Reflection.PropertyInfo property,
            StoreConventionEngine engine,
            string engineKey)
        {
            // Get cached property context
            var propertyContext = EntityConventionCache.GetOrCreatePropertyContext(
                engineKey,
                entityType,
                property.Name,
                () => engine.ApplyPropertyConventions(entityType, property));

            if (!propertyContext.NeedApplyPropertyConvention)
                return;

            var entityBuilder = modelBuilder.Entity(entityType);

            ApplyIgnoredPropertyConvention(entityBuilder, property.Name, propertyContext);
            ApplyDefaultValueConvention(entityBuilder, property.Name, propertyContext);
        }

        private static void ApplyIgnoredPropertyConvention(
            EntityTypeBuilder entityBuilder,
            string propertyName,
            PropertyConventionContext propertyContext)
        {
            if (!propertyContext.IsIgnored)
                return;

            var existingProperty = entityBuilder.Metadata.FindProperty(propertyName);
            if (existingProperty == null && !IsNavigationProperty(entityBuilder.Metadata, propertyName))
            {
                entityBuilder.Ignore(propertyName);
            }
        }

        private static void ApplyDefaultValueConvention(
            EntityTypeBuilder entityBuilder,
            string propertyName,
            PropertyConventionContext propertyContext)
        {
            if (!propertyContext.HasDefaultValue)
                return;

            var existingProperty = entityBuilder.Metadata.FindProperty(propertyName);
            var currentDefaultValue = existingProperty?.GetDefaultValue();
            if (currentDefaultValue == null)
            {
                entityBuilder.Property(propertyName).HasDefaultValue(propertyContext.DefaultValue);
            }
        }

        private static bool IsNavigationProperty(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType, string propertyName)
        {
            return entityType.GetNavigations().Any(n => n.Name == propertyName) ||
                   entityType.GetSkipNavigations().Any(n => n.Name == propertyName);
        }

        /// <summary>
        /// Clear all cached convention results (useful for testing).
        /// The configured convention engine is kept so later model builds still receive conventions.
        /// </summary>
        public static void ClearConventionCache()
        {
            EntityConventionCache.Clear();
        }
    }
}