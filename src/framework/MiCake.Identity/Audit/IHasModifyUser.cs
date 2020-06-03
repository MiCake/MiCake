namespace MiCake.Audit
{
    /// <summary>
    /// Represents has a modify user. If audit is enabled, it will be assigned to the audited system later.
    /// </summary>
    /// <typeparam name="TKey">the primary key type of user.Must be consistent with <see cref="IMiCakeUser{TKey}"/></typeparam>
    public interface IHasModifyUser<TKey> : IHasAuditUser
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey ModifyUserID { get; set; }
    }
}
