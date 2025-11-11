using MiCake.DDD.Infrastructure.Store;
using System;

namespace MiCake.Audit.Conventions
{
    /// <summary>
    /// Convention for audit time properties
    /// </summary>
    public class AuditTimeConvention : IPropertyConvention
    {
        public int Priority => 200;
        
        public bool CanApply(Type entityType)
        {
            return typeof(IHasCreationTime).IsAssignableFrom(entityType) ||
                   typeof(IHasModificationTime).IsAssignableFrom(entityType);
        }
        
        public void Configure(Type entityType, string propertyName, PropertyConventionContext context)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            ArgumentNullException.ThrowIfNull(propertyName);
            ArgumentNullException.ThrowIfNull(context);

            // These properties should be handled by audit providers, not store defaults
            if (propertyName == nameof(IHasCreationTime.CreationTime) ||
                propertyName == nameof(IHasModificationTime.ModificationTime))
            {
                // dont need to do anything, just ignore
                // usually, these properties are handled by efcore correctly
                
                return;
            }
        }
    }
}