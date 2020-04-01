using MiCake.Audit.SoftDeletion;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    class HasAuditWithSoftDeletionModel : IHasAuditWithSoftDeletion
    {
        public DateTime CreationTime { get ; set ; }
        public DateTime? ModificationTime { get ; set ; }
        public bool IsDeleted { get ; set ; }
        public DateTime? DeletionTime { get ; set ; }
    }
}
