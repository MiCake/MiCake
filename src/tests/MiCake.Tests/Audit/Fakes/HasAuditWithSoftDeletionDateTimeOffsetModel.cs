using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    /// <summary>
    /// Test entity with full audit support using DateTimeOffset.
    /// </summary>
    public class HasAuditWithSoftDeletionDateTimeOffsetModel : Entity, IAuditableWithSoftDeletion<DateTimeOffset>
    {
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
