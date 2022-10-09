using MiCake.Audit.SoftDeletion;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    class HasAuditWithSoftDeletionModel : IHasAuditTimeWithSoftDeletion
    {
        public bool IsDeleted { get; set; }

        public DateTime CreatedTime
        {
            get; set;
        }

        public DateTime? UpdatedTime
        {
            get; set;
        }

        public DateTime? DeletedTime
        {
            get; set;
        }
    }
}
