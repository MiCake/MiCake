namespace MiCake.Audit.Core
{
    /// <summary>
    /// Define a class  provide audit way.
    /// </summary>
    public interface IAuditProvider
    {
        /// <summary>
        /// Audit according to the information of audit object
        /// </summary>
        /// <param name="auditObjectModel"><see cref="AuditObjectModel"/></param>
        void ApplyAudit(AuditObjectModel auditObjectModel);
    }
}
