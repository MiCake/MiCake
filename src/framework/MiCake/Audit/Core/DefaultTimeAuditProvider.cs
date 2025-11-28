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

        public virtual void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            if (auditObjectModel?.AuditEntity == null)
                return;

            switch (auditObjectModel.EntityState)
            {
                case RepositoryEntityState.Added:
                    SetCreationTime(auditObjectModel.AuditEntity);
                    break;

                case RepositoryEntityState.Modified:
                    SetModificationTime(auditObjectModel.AuditEntity);
                    break;
            }
        }

        private static void SetCreationTime(object entity)
        {
            if (entity == null)
                return;

            if (entity is IHasCreationTime hasCreationTime &&
                hasCreationTime.CreationTime == default)
            {
                hasCreationTime.CreationTime = CurrentTimeProvider?.Invoke() ?? DateTime.Now;
            }
        }

        private static void SetModificationTime(object entity)
        {
            if (entity == null)
                return;

            if (entity is IHasModificationTime hasModificationTime)
            {
                hasModificationTime.ModificationTime = CurrentTimeProvider?.Invoke() ?? DateTime.Now;
            }
        }
    }
}
