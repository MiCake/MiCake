using MiCake.Audit.SoftDeletion;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    class HasAuditWithSoftDeletionModel : IHasAuditWithSoftDeletion
    {
        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletionTime { get; set; }
    }
}
