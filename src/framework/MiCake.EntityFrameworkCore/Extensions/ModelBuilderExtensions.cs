using Microsoft.EntityFrameworkCore;
using MiCake.DDD.Extensions.Store;
using MiCake.DDD.Extensions.Store.Conventions;
using System;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Extensions
{
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Apply MiCake store conventions to the model
        /// </summary>
        public static void ApplyMiCakeConventions(this ModelBuilder modelBuilder, params Type[] entityTypes)
        {
            var engine = CreateDefaultConventionEngine();
            
            foreach (var entityType in entityTypes)
            {
                ApplyEntityConventions(modelBuilder, entityType, engine);
            }
        }
        
        private static StoreConventionEngine CreateDefaultConventionEngine()
        {
            var engine = new StoreConventionEngine();
            
            // Register built-in conventions
            engine.AddConvention(new SoftDeletionConvention());
            engine.AddConvention(new AuditTimeConvention());
            
            return engine;
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