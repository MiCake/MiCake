using MiCake.Identity;

namespace MiCake.Audit
{
    /// <summary>
    /// Represents has a creator.If audit is enabled, it will be assigned to the audited system later.
    /// 
    /// <para>
    ///     this interface is limit primary key type must be a struct.
    ///     if you want to use a non-structure primary key, you can use <see cref="IMayHasCreator{TKey}"/>
    /// </para>
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the user.Must be consistent with <see cref="IMiCakeUser"/></typeparam>
    public interface IHasCreator<TKey> : IHasAuditUser where TKey : struct
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? CreatorID { get; set; }
    }

    public interface IMayHasCreator<TKey> : IHasAuditUser where TKey : class
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? CreatorID { get; set; }
    }
}
