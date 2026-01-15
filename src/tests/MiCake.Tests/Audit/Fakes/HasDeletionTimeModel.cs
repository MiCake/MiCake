using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasDeletionTimeModel : Entity, IHasDeletedAt, ISoftDeletable
    {
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
