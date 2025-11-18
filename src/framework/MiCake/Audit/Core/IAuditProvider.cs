namespace MiCake.Audit.Core
{
    /// <summary>
    /// Audit provider to apply audit information to entities.
    /// 
    /// <para>
    /// The default implementation is <see cref="DefaultTimeAuditProvider"/>, which handles setting creation and modification times for entities.
    /// </para>
    /// <para>
    /// Custom implementations can be created by implementing this interface to provide specific audit logic as needed, 
    /// and replacing the default provider in the DI container.
    /// </para>
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
