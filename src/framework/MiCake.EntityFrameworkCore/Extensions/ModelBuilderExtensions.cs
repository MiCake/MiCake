using Microsoft.EntityFrameworkCore;
using MiCake.DDD.Extensions.Store;
using MiCake.Core.Util;
using System;
using System.Linq;

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
        private static StoreConventionEngine _conventionEngine;
        
        internal static void SetConventionEngine(StoreConventionEngine engine)
        {
            _conventionEngine = engine;
        }
        
        internal static StoreConventionEngine GetConventionEngine()
        {
            return _conventionEngine;
        }
        
        internal static void Clear()
        {
            _conventionEngine = null;
        }
    }

    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Apply MiCake store conventions to the model using the configured convention engine
        /// </summary>
        public static void ApplyMiCakeConventions(this ModelBuilder modelBuilder, params Type[] entityTypes)
        {
            if (entityTypes == null || entityTypes.Length == 0)
                return;

            // Get the configured convention engine from MiCake container
            var engine = MiCakeConventionEngineProvider.GetConventionEngine();
            if (engine == null)
            {
                // No conventions configured, skip processing
                return;
            }
            
            ApplyConventionsInternal(modelBuilder, entityTypes, engine);
        }
        
        private static void ApplyConventionsInternal(ModelBuilder modelBuilder, Type[] entityTypes, StoreConventionEngine engine)
        {
            var engineKey = engine.GetHashCode().ToString();
            
            foreach (var entityType in entityTypes)
            {
                try
                {
                    // First check if any convention can apply to this entity type using CanApply
                    if (!CanAnyConventionApply(engine, entityType))
                        continue;
                        
                    ApplyEntityConventions(modelBuilder, entityType, engine, engineKey);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("cannot be configured as an entity"))
                {
                    // Entity is configured as owned/complex type by user - skip
                    System.Diagnostics.Debug.WriteLine($"Skipping conventions for {entityType.Name} - configured as owned/complex type");
                    continue;
                }
                catch (Exception ex)
                {
                    // Log other exceptions but continue processing
                    System.Diagnostics.Debug.WriteLine($"Failed to apply conventions for {entityType.Name}: {ex.Message}");
                    continue;
                }
            }
        }
        
        private static bool CanAnyConventionApply(StoreConventionEngine engine, Type entityType)
        {
            // Use the engine's built-in method to check if any convention can apply
            return engine.CanApplyToEntityType(entityType);
        }
        
        private static void ApplyEntityConventions(ModelBuilder modelBuilder, Type entityType, StoreConventionEngine engine, string engineKey)
        {
            // Check if entity exists in the model after user configuration
            var existingEntityType = modelBuilder.Model.FindEntityType(entityType);
            if (existingEntityType == null)
            {
                // Entity was not configured by user, we can try to add it
                try
                {
                    modelBuilder.Entity(entityType);
                    existingEntityType = modelBuilder.Model.FindEntityType(entityType);
                }
                catch (InvalidOperationException)
                {
                    // Cannot add entity (probably abstract or interface), skip
                    return;
                }
            }
            
            var isOwnedEntity = existingEntityType?.IsOwned() == true;
            
            // Get cached entity context
            var entityContext = EntityConventionCache.GetOrCreateEntityContext(engineKey, entityType, 
                () => engine.ApplyEntityConventions(entityType));
            
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder entityBuilder;
            
            try
            {
                entityBuilder = modelBuilder.Entity(entityType);
            }
            catch (InvalidOperationException)
            {
                // Cannot get entity builder (probably owned type), skip
                System.Diagnostics.Debug.WriteLine($"Cannot get entity builder for {entityType.Name}, skipping conventions");
                return;
            }
            
            // Apply entity-level conventions (only for non-owned entities)
            if (!isOwnedEntity)
            {
                ApplyEntityLevelConventions(entityBuilder, entityContext);
            }
            
            // Apply property-level conventions for both owned and regular entities
            ApplyPropertyLevelConventions(entityBuilder, entityType, engine, engineKey, isOwnedEntity);
        }
        
        private static void ApplyEntityLevelConventions(
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder entityBuilder, 
            EntityConventionContext entityContext)
        {
            try
            {
                // Apply soft deletion query filter
                if (entityContext.EnableSoftDeletion && entityContext.QueryFilter != null)
                {
                    var existingFilter = entityBuilder.Metadata.GetQueryFilter();
                    if (existingFilter == null)
                    {
                        entityBuilder.HasQueryFilter(entityContext.QueryFilter);
                    }
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to apply entity-level conventions: {ex.Message}");
            }
        }
        
        private static void ApplyPropertyLevelConventions(
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder entityBuilder, 
            Type entityType, 
            StoreConventionEngine engine,
            string engineKey,
            bool isOwnedEntity)
        {
            var properties = entityType.GetProperties();
            
            foreach (var property in properties)
            {
                try
                {
                    // Get cached property context
                    var propertyContext = EntityConventionCache.GetOrCreatePropertyContext(
                        engineKey,
                        entityType, 
                        property.Name,
                        () => engine.ApplyPropertyConventions(entityType, property));
                    
                    // Skip ignored properties for owned entities (they might be navigation properties)
                    if (propertyContext.IsIgnored && !isOwnedEntity)
                    {
                        var existingProperty = entityBuilder.Metadata.FindProperty(property.Name);
                        if (existingProperty == null && !IsNavigationProperty(entityBuilder.Metadata, property.Name))
                        {
                            entityBuilder.Ignore(property.Name);
                        }
                    }
                    
                    // Apply property configurations
                    ApplyPropertyConfiguration(entityBuilder, property.Name, propertyContext);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to apply property conventions for {entityType.Name}.{property.Name}: {ex.Message}");
                    continue;
                }
            }
        }
        
        private static void ApplyPropertyConfiguration(
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder entityBuilder,
            string propertyName,
            PropertyConventionContext propertyContext)
        {
            var existingProperty = entityBuilder.Metadata.FindProperty(propertyName);
            
            // Apply default value if property exists and doesn't have a default value set
            if (propertyContext.HasDefaultValue && existingProperty != null)
            {
                var currentDefaultValue = existingProperty.GetDefaultValue();
                if (currentDefaultValue == null)
                {
                    entityBuilder.Property(propertyName).HasDefaultValue(propertyContext.DefaultValue);
                }
            }
        }
        
        private static bool IsNavigationProperty(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType, string propertyName)
        {
            return entityType.GetNavigations().Any(n => n.Name == propertyName) ||
                   entityType.GetSkipNavigations().Any(n => n.Name == propertyName);
        }
        
        /// <summary>
        /// Clear all cached convention data (useful for testing)
        /// </summary>
        public static void ClearConventionCache()
        {
            EntityConventionCache.Clear();
            MiCakeConventionEngineProvider.Clear();
        }
    }
}