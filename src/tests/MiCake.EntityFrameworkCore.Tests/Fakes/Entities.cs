using MiCake.DDD.Domain;
using System.Collections.Generic;

namespace MiCake.EntityFrameworkCore.Tests.Fakes
{
    public class NormalEntity : Entity
    {
        public string Name { get; set; }
    }

    public class NormalAggregateRoot : AggregateRoot
    {
        public string Name { get; set; }
    }

    public class ColorValueObject : ValueObject
    {
        public int R { get; private set; }

        public int G { get; private set; }

        public int B { get; private set; }

        public ColorValueObject()
        {
        }

        public ColorValueObject(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return R;
        }
    }

    public class DemoDomainEvent : IDomainEvent
    {
        public string Name { get; set; }
    }
}
