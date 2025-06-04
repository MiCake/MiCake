using MiCake.Audit.SoftDeletion;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    class HasDeletionTimeModel_NoSoftDeletion : IHasDeletionTime
    {
        public DateTime? DeletionTime { get; set; }
    }
}
