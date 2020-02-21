namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with has Modification time and Modifier
    /// </summary>
    public interface IModificationAudit : IHasModificationTime, IHasModifier
    {
    }

    /// <summary>
    /// Mark a class with has Modification time and Modifier
    /// </summary>
    /// <typeparam name="TUserKeyType">a primary type for your user class</typeparam>
    public interface IModificationAudit<TUserKeyType> : IHasModificationTime, IHasModifier<TUserKeyType>
    {
    }
}
