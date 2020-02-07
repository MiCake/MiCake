using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.DDD.Tests.Fakes.ValueObjects
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
    }
}
