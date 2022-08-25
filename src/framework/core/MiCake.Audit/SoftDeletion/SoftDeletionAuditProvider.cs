using MiCake.Audit.Core;
using MiCake.Cord;
using MiCake.Core.Time;
using MiCake.Core.Util.Reflection;
using Microsoft.Extensions.Options;

namespace MiCake.Audit.SoftDeletion
{
    internal class SoftDeletionAuditProvider : IAuditProvider
    {
        private readonly AuditCoreOptions _options;
        private readonly IAppClock _clock;

        public SoftDeletionAuditProvider(IOptions<AuditCoreOptions> options, IAppClock appClock)
        {
            _options = options.Value;
            _clock = appClock;
        }

        public virtual void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            if (auditObjectModel.EntityState != RepositoryEntityState.Deleted)
                return;

            var entity = auditObjectModel.AuditEntity;
            if (entity is not ISoftDeletion)
                return;

            ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(ISoftDeletion.IsDeleted), true);

            if (entity is IHasDeletedTime)
            {
                var value = _options.DateTimeValueProvider is null ? _clock.Now : _options.DateTimeValueProvider();
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasDeletedTime.DeletedTime), value);
            }
        }
    }
}
