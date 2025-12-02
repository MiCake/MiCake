using MiCake.DDD.Infrastructure;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// A contract for executing audit operations which will use different <see cref="IAuditProvider"/> to apply audit.
    /// </summary>
    internal interface IAuditExecutor
    {
        /// <summary>
        /// Execute audit operation for the given entity and its state.
        /// </summary>
        /// <param name="needAuditEntity">The entity that requires auditing.</param>
        /// <param name="entityState"><see cref="RepositoryEntityState"/></param>
        void Execute(object needAuditEntity, RepositoryEntityState entityState);
    }
}
