using MiCake.Cord;
using MiCake.Core.Time;
using MiCake.Core.Util.Reflection;
using Microsoft.Extensions.Options;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Give entity creation time or modifaction time.
    /// </summary>
    internal class DefaultTimeAuditProvider : IAuditProvider
    {
        private readonly AuditCoreOptions _options;
        private readonly IAppClock _clock;

        public DefaultTimeAuditProvider(IOptions<AuditCoreOptions> options, IAppClock clock)
        {
            _options = options.Value;
            _clock = clock;
        }

        public virtual void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            if (_options.AssignCreatedTime && auditObjectModel.EntityState == RepositoryEntityState.Added)
            {
                SetCreationTime(auditObjectModel.AuditEntity);
            }

            if (_options.AssignUpdatedTime && auditObjectModel.EntityState == RepositoryEntityState.Modified)
            {
                SetModifactionTime(auditObjectModel.AuditEntity);
            }
        }

        private void SetCreationTime(object auditEntity)
        {
            if (auditEntity is not IHasCreatedTime)
                return;

            var value = _options.DateTimeValueProvider is null ? _clock.Now : _options.DateTimeValueProvider();
            ReflectionHelper.SetValueByPath(auditEntity, auditEntity.GetType(), nameof(IHasCreatedTime.CreatedTime), value);
        }

        private void SetModifactionTime(object entity)
        {
            if (entity is not IHasUpdatedTime)
                return;

            var value = _options.DateTimeValueProvider is null ? _clock.Now : _options.DateTimeValueProvider();
            ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasUpdatedTime.UpdatedTime), value);
        }
    }
}
