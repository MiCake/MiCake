using System;

using static MiCake.Core.Util.Converts.ConvertHelper;

namespace MiCake.Core.Util.Tests
{
    public class ConvertHelp_Tests
    {
        public ConvertHelp_Tests()
        {
        }

        [Fact]
        public void Convert_PrimitiveType()
        {
            int strToInt = ConvertValue<string, int>("123");
            Assert.Equal(123, strToInt);

            string intToString = ConvertValue<int, string>(123);
            Assert.Equal("123", intToString);

            //Due to the use of Convert.ChangeType So you can convert most primitive types:
            //Boolean Char SByte Byte Int16 UInt16 Int32 UInt32 Int64 UInt64 Single Double Decimal DateTime String Object

            var strToDateTime = ConvertValue<string, DateTime>("2020-02-02");
            Assert.Equal(2020, strToDateTime.Year);

            var wrongStrToDateTime = ConvertValue<string, DateTime>("xxx");
            Assert.Equal(default, wrongStrToDateTime);
        }

        [Fact]
        public void Convert_StringToGuidType()
        {
            var originalGuid = Guid.NewGuid();
            var strToGuid = ConvertValue<string, Guid>(originalGuid.ToString());

            Assert.Equal(originalGuid, strToGuid);
        }

        [Fact]
        public void Convert_OtherTypeToGuidType()
        {
            string originalGuid = "ecdf0e29-6952-47e9-b304-0cb0bea0e9b8";
            var spanType = new ReadOnlySpan<char>(originalGuid.ToCharArray());

            Assert.True(Guid.TryParse(spanType, out var result));   //this is true.

            var otherTypeToGuid = ConvertValue<object, Guid>(originalGuid.ToCharArray());
            //but convert util can not convert right value to guid type.
            //This will become a legacy feature
            Assert.Equal(default, otherTypeToGuid);
        }

        [Fact]
        public void Convert_StringToVersionType()
        {
            var strToVersion = ConvertValue<string, Version>("1.0.0");
            Assert.Equal(1, strToVersion.Major);
        }
    }
}