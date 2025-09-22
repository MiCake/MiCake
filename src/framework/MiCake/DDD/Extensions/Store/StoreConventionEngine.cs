using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Engine for applying store conventions to entities
    /// </summary>
    public class StoreConventionEngine
    {
        private readonly List<IStoreConvention> _conventions = new List<IStoreConvention>();
        
        public void AddConvention(IStoreConvention convention)
        {
            if (convention == null)
                throw new ArgumentNullException(nameof(convention));
                
            _conventions.Add(convention);
            _conventions.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        }
        
        public EntityConventionContext ApplyEntityConventions(Type entityType)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
                
            var context = new EntityConventionContext();
            
            var entityConventions = _conventions
                .OfType<IEntityConvention>()
                .Where(c => c.CanApply(entityType));
                
            foreach (var convention in entityConventions)
            {
                convention.Configure(entityType, context);
            }
            
            return context;
        }
        
        public PropertyConventionContext ApplyPropertyConventions(Type entityType, PropertyInfo property)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
            if (property == null)
                throw new ArgumentNullException(nameof(property));
                
            var context = new PropertyConventionContext();
            
            var propertyConventions = _conventions
                .OfType<IPropertyConvention>()
                .Where(c => c.CanApply(entityType));
                
            foreach (var convention in propertyConventions)
            {
                convention.Configure(entityType, property.Name, context);
            }
            
            return context;
        }
    }
}