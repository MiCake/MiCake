using MiCake.Audit.Core;
using MiCake.DDD.Infrastructure;
using System;

namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Provides soft deletion audit functionality for entities.
    /// </summary>
    internal class SoftDeletionAuditProvider(TimeProvider? timeProvider = null) : IAuditProvider
    {
        private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

        public virtual void ApplyAudit(AuditOperationContext context)
        {
            if (context?.Entity == null)
                return;

            if (context.EntityState != RepositoryEntityStates.Deleted)
                return;

            var entity = context.Entity;
            if (entity is not ISoftDeletable softDeletionObj)
                return;

            softDeletionObj.IsDeleted = true;

            var now = _timeProvider.GetUtcNow();
            if (entity is IHasDeletedAt<DateTimeOffset> hasDeletionTimeOffset)
            {
                hasDeletionTimeOffset.DeletedAt = now;
                return;
            }

            if (entity is IHasDeletedAt<DateTime> hasDeletionTime)
            {
                hasDeletionTime.DeletedAt = now.DateTime;
            }
        }
    }
}
