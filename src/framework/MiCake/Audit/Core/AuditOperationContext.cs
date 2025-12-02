using MiCake.DDD.Infrastructure;
using System.Collections.Generic;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Context information for an audit operation.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="state"></param>
    public class AuditOperationContext(object entity, RepositoryEntityState state)
    {
        /// <summary>
        /// The entity being audited, It should be the instance of domain object.
        /// </summary>
        public object Entity { get; set; } = entity;

        /// <summary>
        /// The state of the entity.
        /// </summary>
        public RepositoryEntityState EntityState { get; set; } = state;

        /// <summary>
        /// Some additional information.
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = [];
    }
}
