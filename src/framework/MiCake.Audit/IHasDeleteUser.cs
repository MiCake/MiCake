namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class has delete user
    /// </summary>
    public interface IHasDeleteUser : IHasAuditUser
    {
        long DeleteUserID { get; set; }
    }

    /// <summary>
    /// Mark a class has delete user
    /// </summary>
    /// <typeparam name="TUserKeyType">the primary key type of user</typeparam>
    public interface IHasDeleteUser<TUserKeyType> : IHasAuditUser
    {
        TUserKeyType DeleteUserID { get; set; }
    }
}
