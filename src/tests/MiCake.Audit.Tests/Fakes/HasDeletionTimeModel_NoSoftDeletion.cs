using MiCake.Audit.SoftDeletion;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    class HasDeletionTimeModel_NoSoftDeletion : IHasDeletedTime
    {
        public DateTime? DeletedTime { get; set; }
    }
}
