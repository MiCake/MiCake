namespace MiCake.Audit
{
    /// <summary>
    /// Define a class has modification time.
    /// </summary>
    public interface IHasModificationTime
    {
        DateTime? ModificationTime { get; }
    }
}
