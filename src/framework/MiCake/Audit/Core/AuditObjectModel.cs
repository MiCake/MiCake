using MiCake.DDD.Extensions;
using System.Collections.Generic;

namespace MiCake.Audit.Core
{
    public class AuditObjectModel
    {
        /// <summary>
        /// The entity who need audited.
        /// </summary>
        public object AuditEntity { get; set; }

        /// <summary>
        /// The state of entity.
        /// </summary>
        public RepositoryEntityState EntityState { get; set; }

        /// <summary>
        /// Some additional information.
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();

        public AuditObjectModel(object entity, RepositoryEntityState state)
        {
            AuditEntity = entity;
            EntityState = state;
        }
    }
}
