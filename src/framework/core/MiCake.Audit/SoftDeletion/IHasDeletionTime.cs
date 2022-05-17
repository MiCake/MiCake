namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Define a class has deletion time.
    /// </summary>
    public interface IHasDeletionTime
    {
        DateTime? DeletionTime { get; set; }
    }
}
