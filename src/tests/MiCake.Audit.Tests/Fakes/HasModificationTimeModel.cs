using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasModificationTimeModel : Entity, IHasModificationTime
    {
        public DateTime? ModificationTime { get; set; }
    }
}
