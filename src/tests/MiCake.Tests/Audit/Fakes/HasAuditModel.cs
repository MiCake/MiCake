using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasAuditModel : Entity, IHasAuditTimestamps
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
