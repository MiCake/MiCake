using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    /// <summary>
    /// Test entity with DateTimeOffset deletion time and soft deletion support.
    /// </summary>
    public class HasDeletionTimeDateTimeOffsetModel : Entity, ISoftDeletable, IHasDeletedAt<DateTimeOffset>
    {
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
