using MiCake.Audit.SoftDeletion;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    class HasDeletionTimeModel_NoSoftDeletion : IHasDeletedAt
    {
        public DateTime? DeletedAt { get; set; }
    }
}
