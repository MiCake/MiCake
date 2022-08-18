namespace MiCake.Audit
{
    /// <summary>
    /// Define a class has creation time.
    /// </summary>
    public interface IHasCreatedTime
    {
        DateTime CreatedTime { get; }
    }
}
