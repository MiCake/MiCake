namespace MiCake.Audit
{
    /// <summary>
    /// Define a class has modification time.
    /// </summary>
    public interface IHasUpdatedTime
    {
        DateTime? UpdatedTime { get; }
    }
}
