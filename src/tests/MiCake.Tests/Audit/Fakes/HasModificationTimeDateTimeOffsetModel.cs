using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    /// <summary>
    /// Test entity with DateTimeOffset modification time.
    /// </summary>
    public class HasModificationTimeDateTimeOffsetModel : Entity, IHasUpdatedAt<DateTimeOffset>
    {
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
