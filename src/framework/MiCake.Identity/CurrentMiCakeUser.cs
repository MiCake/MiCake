namespace MiCake.Identity
{
    /// <summary>
    /// The abstract class for <see cref="ICurrentMiCakeUser"/>
    /// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
    /// </summary>
    public abstract class CurrentMiCakeUser<TKey> : ICurrentMiCakeUser<TKey>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public TKey UserID { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        object ICurrentMiCakeUser.UserID => this.UserID;

        public CurrentMiCakeUser()
        {
            UserID = GetUserID();
        }

        public abstract TKey GetUserID();
    }
}
