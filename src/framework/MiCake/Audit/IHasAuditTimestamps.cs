namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with the timestamps of creation and modification.
    /// <para>
    /// It is the combination of <see cref="IHasCreationTime"/> and <see cref="IHasModificationTime"/>.
    /// </para>
    /// </summary>
    public interface IHasAuditTimestamps : IHasCreationTime, IHasModificationTime
    {
    }
}
