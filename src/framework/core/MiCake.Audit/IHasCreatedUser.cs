using MiCake.Identity;

namespace MiCake.Audit
{
    /// <summary>
    /// Represents has a created user.If audit is enabled, it will be assigned to the audited system later.
    /// 
    /// <para>
    ///     this interface is limit primary key type must be a struct.
    ///     if you want to use a non-structure primary key, you can use <see cref="IMayHasCreatedUser{TKey}"/>
    /// </para>
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the user.Must be consistent with <see cref="IMiCakeUser"/></typeparam>
    public interface IHasCreatedUser<TKey> : ICanAuditUser where TKey : struct
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? CreatedBy { get; }
    }

    public interface IMayHasCreatedUser<TKey> : ICanAuditUser where TKey : class
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? CreatedBy { get; }
    }
}
