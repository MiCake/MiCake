using MiCake.Cord.Tests.Fakes.ValueObjects;
using Xunit;

namespace MiCake.Cord.Tests
{
    public class ValueObject_Tests
    {
        [Fact]
        public void ValueObject_SameTypeCompare_Test()
        {
            ValueObjectA mixiObject = new("mi", "xi");
            ValueObjectA mixi2Object = new("mi", "xi");

            Assert.Equal(mixiObject, mixi2Object);
        }

        [Fact]
        public void ValueObject_Operator_Test()
        {
            ValueObjectA mixiObject = new("mi", "xi");
            ValueObjectA mixi2Object = new("mi", "xi");
            ValueObjectA mixi3Object = new("mii", "xii");

            var compareResult = mixiObject == mixi2Object;
            Assert.True(compareResult);

            var compareResult2 = mixiObject != mixi3Object;
            Assert.True(compareResult2);
        }

        [Fact]
        public void Entity_Equal_Test()
        {
            ValueObjectA mixiObject = new("mi", "xi");

            Assert.False(mixiObject.Equals(null));
            Assert.False(mixiObject.Equals(new object()));
            Assert.True(mixiObject.Equals(mixiObject));
        }
    }
}
