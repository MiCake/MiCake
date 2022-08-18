using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasCreationTimeModel : Entity, IHasCreatedTime
    {
        public DateTime CreatedTime { get; set; }
    }
}
