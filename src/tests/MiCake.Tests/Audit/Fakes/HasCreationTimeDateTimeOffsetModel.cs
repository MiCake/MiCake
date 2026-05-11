using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    /// <summary>
    /// Test entity with DateTimeOffset creation time.
    /// </summary>
    public class HasCreationTimeDateTimeOffsetModel : Entity, IHasCreatedAt<DateTimeOffset>
    {
        public DateTimeOffset CreatedAt { get; set; }
    }
}
