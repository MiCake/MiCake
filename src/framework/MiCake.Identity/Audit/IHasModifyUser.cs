namespace MiCake.Identity.Audit
{
    public interface IHasModifyUser : IHasAuditUser
    {
        long? ModifyUserID { get; set; }
    }

    public interface IHasModifyUser<TUserKeyType> : IHasAuditUser
    {
        TUserKeyType ModifyUserID { get; set; }
    }
}
