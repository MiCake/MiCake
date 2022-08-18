using MiCake.Cord;
using MiCake.Core.Util.Reflection;
using Microsoft.Extensions.Options;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Give entity creation time or modifaction time.
    /// </summary>
    internal class DefaultTimeAuditProvider : IAuditProvider
    {
        private AuditCoreOptions _options;

        public DefaultTimeAuditProvider(IOptions<AuditCoreOptions> options)
        {
            _options = options.Value;
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

            var value = _options.DateTimeValueProvider is null ? DateTime.UtcNow : _options.DateTimeValueProvider();
            ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasUpdatedTime.UpdatedTime), value);
        }
    }
}
