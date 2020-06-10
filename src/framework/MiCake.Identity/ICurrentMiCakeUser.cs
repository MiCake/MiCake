namespace MiCake.Identity
{
    /// <summary>
    /// Get current <see cref="IMiCakeUser"/> some info.
    /// </summary>
    public interface ICurrentMiCakeUser
    {
        /// <summary>
        /// The id of current user.
        /// </summary>
        public object UserID { get; }
    }

    /// <summary>
    /// Get current <see cref="IMiCakeUser"/> some info.
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
    public interface ICurrentMiCakeUser<TKey> : ICurrentMiCakeUser
    {
        /// <summary>
        /// The id of current user.
        /// </summary>
        public new TKey UserID { get; }
    }
}
