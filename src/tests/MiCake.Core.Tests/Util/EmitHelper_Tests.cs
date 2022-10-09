using MiCake.Core.Util.Reflection;
using System;

namespace MiCake.Core.Util.Tests
{
    public class EmitHelper_Tests
    {
        public EmitHelper_Tests()
        {
        }

        [Fact]
        public void CreateClass_Test()
        {
            var type = EmitHelper.CreateClass("TestClass").CreateType();

            Assert.Equal("TestClass", type.Name);
        }

        [Fact]
        public void CreatepProperty_Test()
        {
            var typeBuilder = EmitHelper.CreateClass("TestClass");
            EmitHelper.CreateProperty(typeBuilder, "Name", typeof(string));

            var type = typeBuilder.CreateType();
            var instance = Activator.CreateInstance(type);

            var propertyInfo = instance.GetType().GetProperty("Name");
            Assert.NotNull(propertyInfo);

            propertyInfo.SetValue(instance, "MiCake");
            Assert.Equal("MiCake", propertyInfo.GetValue(instance));
        }
    }
}
