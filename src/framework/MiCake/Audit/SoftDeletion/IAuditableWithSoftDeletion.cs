namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Mark a class with audit timestamps and soft deletion properties.
    /// <para>
    /// It is the combination of <see cref="IHasAuditTimestamps"/>, <see cref="ISoftDeletion"/>, and <see cref="IHasDeletionTime"/>.
    /// </para>
    /// </summary>
    public interface IAuditableWithSoftDeletion : IHasAuditTimestamps, ISoftDeletion, IHasDeletionTime
    {
    }
}
