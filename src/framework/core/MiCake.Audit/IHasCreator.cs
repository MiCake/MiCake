using MiCake.Identity;

namespace MiCake.Audit
{
    /// <summary>
    /// Represents has a creator.If audit is enabled, it will be assigned to the audited system later.
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the user.Must be consistent with <see cref="IMiCakeUser"/></typeparam>
    public interface IHasCreator<TKey> : IHasAuditUser
    {
        /// <summary>
        /// The primary key for user.
        /// </summary>
        TKey? CreatorID { get; set; }
    }
}
