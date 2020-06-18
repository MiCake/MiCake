namespace MiCake.Identity
{
    /// <summary>
    /// The abstract class for <see cref="ICurrentMiCakeUser"/>
    /// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
    /// </summary>
    public abstract class CurrentMiCakeUser<TKey> : ICurrentMiCakeUser<TKey>
    {
        private static object @object = new object();
        private bool hasGetUserId = false;

        private TKey _userId;
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual TKey UserId
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
        object ICurrentMiCakeUser.UserId => UserId;

        public CurrentMiCakeUser()
        {
        }

        public abstract TKey GetUserID();
    }
}
