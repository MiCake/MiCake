using MiCake.Identity;

namespace MiCake.Audit
{
    /// <summary>
    /// Represents has a modify user. If audit is enabled, it will be assigned to the audited system later.
    /// 
    /// <para>
    ///     this interface is limit primary key type must be a struct.
    ///     if you want to use a non-structure primary key, you can use <see cref="IMayHasModifyUser{TKey}"/>
    /// </para>
    /// </summary>
    /// <typeparam name="TKey">the primary key type of user.Must be consistent with <see cref="IMiCakeUser{TKey}"/></typeparam>
    public interface IHasModifyUser<TKey> : IHasAuditUser where TKey : struct
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? ModifyUserID { get; }
    }

    public interface IMayHasModifyUser<TKey> : IHasAuditUser where TKey : class
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? ModifyUserID { get; }
    }
}
