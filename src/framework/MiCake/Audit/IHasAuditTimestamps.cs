namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with the timestamps of creation and modification.
    /// <para>
    /// It is the combination of <see cref="IHasCreatedAt"/> and <see cref="IHasUpdatedAt"/>.
    /// </para>
    /// </summary>
    public interface IHasAuditTimestamps : IHasCreatedAt, IHasUpdatedAt
    {
    }
}
