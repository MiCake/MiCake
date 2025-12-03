using MiCake.Audit.SoftDeletion;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    class HasAuditWithSoftDeletionModel : IAuditableWithSoftDeletion
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
