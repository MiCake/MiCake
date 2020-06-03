namespace MiCake.Identity
{
    /// <summary>
    /// Defined a MiCake User interface.
    /// 
    /// <para>
    ///     This interface is only used for marking, you need to use <see cref="IMiCakeUser{TKey}"/>
    /// </para>
    /// </summary>
    public interface IMiCakeUser
    {
    }

    /// <summary>
    /// Represents a user in the miacke identity system
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
    public interface IMiCakeUser<TKey> : IMiCakeUser
    {
        /// <summary>
        ///  Gets or sets the primary key for this user.
        /// </summary>
        public TKey Id { get; set; }
    }
}
