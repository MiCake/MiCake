using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasDeletionTimeModel : Entity, IHasDeletedTime, ISoftDeletion
    {
        public DateTime? DeletedTime { get; set; }
        public bool IsDeleted { get; set; }
    }
}
