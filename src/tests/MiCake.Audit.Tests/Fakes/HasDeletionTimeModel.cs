using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasDeletionTimeModel : Entity, IHasDeletionTime,ISoftDeletion
    {
        public DateTime? DeletionTime { get; set; }
        public bool IsDeleted { get; set; }
    }
}
