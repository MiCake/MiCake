using MiCake.DDD.Domain;
using MiCake.DDD.Tests.Fakes.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MiCake.DDD.Tests
{
    public class ValueObject_Compare_Tests
    {
        [Fact]
        public void ValueObject_SameTypeCompare_Test()
        {
            ValueObjectA mixiObject = new ValueObjectA("mi", "xi");
            ValueObjectA mixi2Object = new ValueObjectA("mi", "xi");

            Assert.Equal(mixiObject, mixi2Object);
        }
    }
}
