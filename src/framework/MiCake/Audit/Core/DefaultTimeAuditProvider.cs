using MiCake.DDD.Infrastructure;
using System;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Provides creation and modification time for entities.
    /// <para>
    /// Supports both <see cref="DateTime"/> and <see cref="DateTimeOffset"/> timestamp types.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="DefaultTimeAuditProvider"/>.
    /// </remarks>
    /// <param name="timeProvider">The time provider. If null, uses <see cref="TimeProvider.System"/>.</param>
    public class DefaultTimeAuditProvider(TimeProvider? timeProvider = null) : IAuditProvider
    {
        private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

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

        private void SetCreationTime(object entity)
        {
            if (entity == null)
                return;

            var now = _timeProvider.GetUtcNow();

            if (entity is IHasCreatedAt<DateTimeOffset> hasCreationTimeOffset &&
                hasCreationTimeOffset.CreatedAt == default)
            {
                hasCreationTimeOffset.CreatedAt = now;
                return;
            }

            if (entity is IHasCreatedAt<DateTime> hasCreationTime &&
                hasCreationTime.CreatedAt == default)
            {
                hasCreationTime.CreatedAt = now.DateTime;
            }
        }

        private void SetModificationTime(object entity)
        {
            if (entity == null)
                return;

            var now = _timeProvider.GetUtcNow();

            if (entity is IHasUpdatedAt<DateTimeOffset> hasModificationTimeOffset)
            {
                hasModificationTimeOffset.UpdatedAt = now;
                return;
            }

            if (entity is IHasUpdatedAt<DateTime> hasModificationTime)
            {
                hasModificationTime.UpdatedAt = now.DateTime;
            }
        }
    }
}
