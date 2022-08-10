namespace MiCake.Identity
{
    /// <summary>
    /// The abstract class for <see cref="ICurrentMiCakeUser"/>
    /// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
    /// </summary>
    public abstract class CurrentMiCakeUser<TKey> : ICurrentMiCakeUser
    {
        private static readonly object @object = new();
        private bool hasGetUserId = false;

        private TKey? _userId;
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual TKey? UserId
        {
            get
            {
                lock (@object)
                {
                    if (hasGetUserId)
                        return _userId;

                    hasGetUserId = true;
                    _userId = GetUserID();

                    return _userId;
                }
            }
            set
            {
                _userId = value;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        object? ICurrentMiCakeUser.UserId => UserId;

        public bool IsLogin => UserId != null && !UserId.Equals(default);

        public CurrentMiCakeUser()
        {
        }

        public abstract TKey? GetUserID();
    }
}
