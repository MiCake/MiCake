using MiCake.Core.Util.Reflection;

namespace MiCake.Core.Tests.Util
{
    public class ReflectionHelper_Tests
    {
        [Fact]
        public void ForceSetPropertyValue()
        {
            var obj = new HavePrivatePropertyDemoClass();

            ReflectionHelper.SetValueByPath(obj, typeof(HavePrivatePropertyDemoClass), nameof(obj.Name), "Zhang");

            Assert.Equal("Zhang", obj.Name);
        }
    }

    class HavePrivatePropertyDemoClass
    {
        public string Name { get; private set; }
    }
}
