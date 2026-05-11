using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    /// <summary>
    /// Test entity with DateTimeOffset audit timestamps.
    /// </summary>
    public class HasAuditDateTimeOffsetModel : Entity, IHasAuditTimestamps<DateTimeOffset>
    {
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
