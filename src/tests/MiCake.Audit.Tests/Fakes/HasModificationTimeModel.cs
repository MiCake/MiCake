using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasModificationTimeModel : Entity, IHasUpdatedTime
    {
        public DateTime? UpdatedTime { get; set; }
    }
}
