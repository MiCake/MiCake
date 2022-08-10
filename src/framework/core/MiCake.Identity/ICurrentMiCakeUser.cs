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
        public object? UserId { get; }

        public bool IsLogin { get; }
    }
}
