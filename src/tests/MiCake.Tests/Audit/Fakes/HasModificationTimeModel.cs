using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasModificationTimeModel : Entity, IHasUpdatedAt
    {
        public DateTime? UpdatedAt { get; set; }
    }
}
