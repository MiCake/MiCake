namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Mark a class with creation info,modify info and deletion info.
    /// If the implementation type is marked as soft delete, the data will not be deleted from the database
    /// </summary>
    public interface IHasAuditTimeWithSoftDeletion : IHasAuditTime, ISoftDeletion, IHasDeletedTime
    {
    }
}
