using MiCake.Audit.Core;
using MiCake.Audit;
using System;

namespace MiCake.DDD.Extensions.Store.Conventions
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
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
                
            // These properties should be handled by audit providers, not store defaults
            if (propertyName == nameof(IHasCreationTime.CreationTime) ||
                propertyName == nameof(IHasModificationTime.ModificationTime))
            {
                // Let audit system handle these properties
                return;
            }
        }
    }
}