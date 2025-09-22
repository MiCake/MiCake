using Microsoft.EntityFrameworkCore;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Conventions;
using System;
using System.Linq;

namespace MiCake.EntityFrameworkCore
{
    public static class EFCoreModelBuilderExtensionSimplified
    {
        /// <summary>
        /// Add MiCake manage model for EFCore using simplified convention system.
        /// This replaces the complex store configuration system with a cleaner approach.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ModelBuilder AddMiCakeModelSimplified(this ModelBuilder builder)
        {
            // Get all entity types from the model
            var entityTypes = builder.Model.GetEntityTypes()
                .Select(t => t.ClrType)
                .Where(t => t != null)
                .ToArray();
                
            // Apply MiCake conventions
            var engine = new StoreConventionEngine();
            engine.AddConvention(new SoftDeletionConvention());
            engine.AddConvention(new AuditTimeConvention());
            
            foreach (var entityType in entityTypes)
            {
                ApplyEntityConventions(builder, entityType, engine);
            }
            
            return builder;
        }
        
        /// <summary>
        /// Add custom conventions to the MiCake convention engine
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="customConventions"></param>
        /// <returns></returns>
        public static ModelBuilder AddMiCakeModelWithCustomConventions(this ModelBuilder builder, params IStoreConvention[] customConventions)
        {
            var engine = new StoreConventionEngine();
            
            // Add built-in conventions
            engine.AddConvention(new SoftDeletionConvention());
            engine.AddConvention(new AuditTimeConvention());
            
            // Add custom conventions
            foreach (var convention in customConventions)
            {
                engine.AddConvention(convention);
            }
            
            // Get all entity types from the model
            var entityTypes = builder.Model.GetEntityTypes()
                .Select(t => t.ClrType)
                .Where(t => t != null)
                .ToArray();
                
            // Apply conventions using the custom engine
            foreach (var entityType in entityTypes)
            {
                ApplyEntityConventions(builder, entityType, engine);
            }
            
            return builder;
        }
        
        private static void ApplyEntityConventions(ModelBuilder modelBuilder, Type entityType, StoreConventionEngine engine)
        {
            var entityContext = engine.ApplyEntityConventions(entityType);
            var entityBuilder = modelBuilder.Entity(entityType);
            
            // Apply soft deletion
            if (entityContext.EnableSoftDeletion)
            {
                // Set query filter
                if (entityContext.QueryFilter != null)
                {
                    entityBuilder.HasQueryFilter(entityContext.QueryFilter);
                }
            }
            
            // Apply ignored properties
            foreach (var ignoredProperty in entityContext.IgnoredProperties)
            {
                entityBuilder.Ignore(ignoredProperty);
            }
            
            // Apply property conventions
            var properties = entityType.GetProperties();
            foreach (var property in properties)
            {
                var propertyContext = engine.ApplyPropertyConventions(entityType, property);
                
                if (propertyContext.IsIgnored)
                {
                    entityBuilder.Ignore(property.Name);
                }
                
                if (propertyContext.HasDefaultValue)
                {
                    entityBuilder.Property(property.Name)
                        .HasDefaultValue(propertyContext.DefaultValue);
                }
            }
        }
    }
}