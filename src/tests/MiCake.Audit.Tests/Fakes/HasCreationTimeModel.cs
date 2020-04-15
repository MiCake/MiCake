using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasCreationTimeModel : Entity, IHasCreationTime
    {
        public DateTime CreationTime { get; set; }
    }
}
