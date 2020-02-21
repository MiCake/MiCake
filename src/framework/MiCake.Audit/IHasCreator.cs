namespace MiCake.Audit
{
    public interface IHasCreator : IHasAuditUser
    {
        /// <summary>
        /// Id of the creator.
        /// </summary>
        long CreatorID { get; set; }
    }

    public interface IHasCreator<TUserKeyType> : IHasAuditUser
    {
        /// <summary>
        /// Id of the creator.
        /// </summary>
        TUserKeyType CreatorID { get; set; }
    }
}
