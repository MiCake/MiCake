using System;

namespace MiCake.DDD.Infrastructure.Store
{
    /// <summary>
    /// Store convention for configuring entity behaviors
    /// </summary>
    public interface IStoreConvention
    {
        /// <summary>
        /// Check if this convention applies to the given entity type
        /// </summary>
        bool CanApply(Type entityType);
        
        /// <summary>
        /// Get convention priority (lower values = higher priority)
        /// </summary>
        int Priority { get; }
    }
    
    /// <summary>
    /// Convention for configuring entity-level behaviors
    /// </summary>
    public interface IEntityConvention : IStoreConvention
    {
        /// <summary>
        /// Configure entity behaviors
        /// </summary>
        void Configure(Type entityType, EntityConventionContext context);
    }
    
    /// <summary>
    /// Convention for configuring property-level behaviors  
    /// </summary>
    public interface IPropertyConvention : IStoreConvention
    {
        /// <summary>
        /// Configure property behaviors
        /// </summary>
        void Configure(Type entityType, string propertyName, PropertyConventionContext context);
    }
}