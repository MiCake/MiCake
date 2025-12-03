namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Mark a class with audit timestamps and soft deletion properties.
    /// <para>
    /// It is the combination of <see cref="IHasAuditTimestamps"/>, <see cref="ISoftDeletable"/>, and <see cref="IHasDeletedAt"/>.
    /// </para>
    /// </summary>
    public interface IAuditableWithSoftDeletion : IHasAuditTimestamps, ISoftDeletable, IHasDeletedAt
    {
    }
}
