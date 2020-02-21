using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class ModelHasDeletionAudit : IDeleteAudit, ISoftDeletion
    {
        public bool IsDeleted { get; set; }
        public long DeleteUserID { get; set; }
        public DateTime? DeletionTime { get; set; }
    }


    public class ModelHasDeletionAuditGeneric : IDeleteAudit<Guid>, ISoftDeletion
    {
        public bool IsDeleted { get; set; }
        public Guid DeleteUserID { get; set; }
        public DateTime? DeletionTime { get; set; }
    }
}
