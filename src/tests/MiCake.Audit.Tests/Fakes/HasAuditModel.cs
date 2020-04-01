using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasAuditModel : Entity, IHasAudit
    {
        public DateTime CreationTime { get ; set ; }
        public DateTime? ModificationTime { get ; set ; }
    }
}
