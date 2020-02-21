namespace MiCake.Identity.User
{
    /// <summary>
    /// This is a statement to the current user.Please use <see cref="ICurrentUser{TKeyType}"/>
    /// </summary>
    public interface ICurrentUser
    {
    }

    /// <summary>
    /// Indicates the current user
    /// </summary>
    /// <typeparam name="TKeyType">the type of user primary key</typeparam>
    public interface ICurrentUser<TKeyType> : ICurrentUser
    {
        TKeyType UserID { get; set; }
    }
}
