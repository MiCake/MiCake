using MiCake.Cord;

namespace MiCake.Audit.Core
{
    /// <summary>
    /// Perform audit process according to <see cref="IAuditProvider"/>
    /// </summary>
    public interface IAuditExecutor
    {
        /// <summary>
        /// Execute audit
        /// </summary>
        /// <param name="needAuditEntity"></param>
        /// <param name="entityState"><see cref="RepositoryEntityState"/></param>
        void Execute(object needAuditEntity, RepositoryEntityState entityState);
    }
}
