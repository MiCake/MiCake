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
            if (auditObjectModel.EntityState == RepositoryEntityState.Modified)
            {
                SetModifactionTime(auditObjectModel.AuditEntity);
            }
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
