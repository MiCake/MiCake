using MiCake.DDD.Domain;
using System.Collections.Generic;

namespace MiCake.Cord.Tests.Fakes.ValueObjects
{
    public class ValueObjectA : ValueObject
    {
        public string Street { get; private set; }
        public string City { get; private set; }

        public ValueObjectA(string street, string city)
        {
            Street = street;
            City = city;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }
}
