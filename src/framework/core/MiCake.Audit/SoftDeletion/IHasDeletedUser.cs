using MiCake.Identity;

namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Represents has a delete user. If audit is enabled, it will be assigned to the audited system later.
    /// <para>
    ///     this interface is limit primary key type must be a struct.
    ///     if you want to use a non-structure primary key, you can use <see cref="IMayHasDeletedUser{TKey}"/>
    /// </para>
    /// </summary>
    /// <typeparam name="TKey">the primary key type of user.Must be consistent with <see cref="IMiCakeUser{TKey}"/></typeparam>
    public interface IHasDeletedUser<TKey> : ICanAuditUser where TKey : struct
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? DeletedBy { get; }
    }

    public interface IMayHasDeletedUser<TKey> : ICanAuditUser where TKey : class
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? DeletedBy { get; }
    }
}
