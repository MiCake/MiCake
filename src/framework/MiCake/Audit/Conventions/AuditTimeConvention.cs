using MiCake.DDD.Infrastructure.Store;
using System;

namespace MiCake.Audit.Conventions
{
    /// <summary>
    /// Convention for audit time properties.
    /// <para>
    /// Supports both legacy interfaces (<see cref="IHasCreatedAt"/>, <see cref="IHasUpdatedAt"/>) 
    /// and generic interfaces (<see cref="IHasCreatedAt{T}"/>, <see cref="IHasUpdatedAt{T}"/>).
    /// Since legacy interfaces inherit from generic ones, a simple type check is sufficient.
    /// </para>
    /// </summary>
    public class AuditTimeConvention : IPropertyConvention
    {
        private static readonly Type DateTimeCreatedAtType = typeof(IHasCreatedAt<DateTime>);
        private static readonly Type DateTimeUpdatedAtType = typeof(IHasUpdatedAt<DateTime>);
        private static readonly Type DateTimeOffsetCreatedAtType = typeof(IHasCreatedAt<DateTimeOffset>);
        private static readonly Type DateTimeOffsetUpdatedAtType = typeof(IHasUpdatedAt<DateTimeOffset>);

        public int Priority => 200;

        public bool CanApply(Type entityType)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            
            return DateTimeCreatedAtType.IsAssignableFrom(entityType) ||
                   DateTimeUpdatedAtType.IsAssignableFrom(entityType) ||
                   DateTimeOffsetCreatedAtType.IsAssignableFrom(entityType) ||
                   DateTimeOffsetUpdatedAtType.IsAssignableFrom(entityType);
        }

        public void Configure(Type entityType, string propertyName, PropertyConventionContext context)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            ArgumentNullException.ThrowIfNull(propertyName);
            ArgumentNullException.ThrowIfNull(context);

            // These properties should be handled by audit providers, not store defaults
            if (propertyName == nameof(IHasCreatedAt.CreatedAt) ||
                propertyName == nameof(IHasUpdatedAt.UpdatedAt))
            {
                // dont need to do anything, just ignore
                // usually, these properties are handled by efcore correctly
            }
        }
    }
}