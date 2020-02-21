namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with creation info and modify info.
    /// </summary>
    public interface IHasAudit : ICreationAudit, IModificationAudit
    {
    }

    /// <summary>
    /// Mark a class with creation info and modify info.
    /// </summary>
    /// <typeparam name="TUserKeyType">a primary type for your user class</typeparam>
    public interface IHasAudit<TUserKeyType> : ICreationAudit<TUserKeyType>, IModificationAudit<TUserKeyType>
    {
    }
}
