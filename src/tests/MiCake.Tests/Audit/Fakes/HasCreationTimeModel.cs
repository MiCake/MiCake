using MiCake.DDD.Domain;
using System;

namespace MiCake.Audit.Tests.Fakes
{
    public class HasCreationTimeModel : Entity, IHasCreatedAt
    {
        public DateTime CreatedAt { get; set; }
    }
}
