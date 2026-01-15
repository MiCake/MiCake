using MiCake.DDD.Infrastructure;
using System;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Provides creation and modification time for entities.
    /// Supports customization of time generation logic through <see cref="CurrentTimeProvider"/>.
    /// </summary>
    public class DefaultTimeAuditProvider : IAuditProvider
    {
        /// <summary>
        /// Gets or sets the function that provides the current time for audit operations.
        /// Default: <see cref="DateTime.UtcNow"/>
        /// </summary>
        public static Func<DateTime> CurrentTimeProvider { get; set; } = () => DateTime.UtcNow;

        public virtual void ApplyAudit(AuditOperationContext context)
        {
            if (context?.Entity == null)
                return;

            switch (context.EntityState)
            {
                case RepositoryEntityStates.Added:
                    SetCreationTime(context.Entity);
                    break;

                case RepositoryEntityStates.Modified:
                    SetModificationTime(context.Entity);
                    break;
            }
        }

        private static void SetCreationTime(object entity)
        {
            if (entity == null)
                return;

            if (entity is IHasCreatedAt hasCreationTime &&
                hasCreationTime.CreatedAt == default)
            {
                hasCreationTime.CreatedAt = CurrentTimeProvider?.Invoke() ?? DateTime.UtcNow;
            }
        }

        private static void SetModificationTime(object entity)
        {
            if (entity == null)
                return;

            if (entity is IHasUpdatedAt hasModificationTime)
            {
                hasModificationTime.UpdatedAt = CurrentTimeProvider?.Invoke() ?? DateTime.UtcNow;
            }
        }
    }
}
