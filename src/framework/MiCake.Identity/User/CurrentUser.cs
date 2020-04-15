namespace MiCake.Identity.User
{
    public abstract class CurrentUser : CurrentUser<long>
    {
    }

    public abstract class CurrentUser<TUserIDType> : ICurrentUser<TUserIDType>
    {
        public abstract TUserIDType UserID { get; set; }
    }
}
