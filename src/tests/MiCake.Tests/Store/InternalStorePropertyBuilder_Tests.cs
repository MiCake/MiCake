using MiCake.Core.Data;
using System;
using Xunit;

namespace MiCake.DDD.Tests.Store
{
    public class InternalStorePropertyBuilder_Tests : StoreConfigTestBase
    {
        public InternalStorePropertyBuilder_Tests()
        {
        }

        [Fact]
        public void Can_Set_Concurrency()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).Property("Name").Concurrency(true);
            Assert.Equal(true, builder.GetAccessor().Metadata.IsConcurrency);
        }

        [Fact]
        public void Can_Set_Nullable()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).Property("Name").Nullable(true);
            Assert.Equal(true, builder.GetAccessor().Metadata.IsNullable);
        }

        [Fact]
        public void Can_Set_MaxLength()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var propertyBuilder = modelbuilder.Entity(typeof(FakeEntityA)).Property("Name");
            Assert.Throws<ArgumentException>(() => propertyBuilder.MaxLength(-1));
            propertyBuilder.MaxLength(100);

            Assert.Equal(100, propertyBuilder.GetAccessor().Metadata.MaxLength);
        }


        [Fact]
        public void Can_Set_DefaultValue()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).Property("Name").DefaultValue("AAA");
            Assert.Equal("AAA", builder.GetAccessor().Metadata.DefaultValue);
        }

        [Fact]
        public void Can_Set_SameProperty_But_OnlyOneType()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var entity = modelbuilder.Entity(typeof(FakeEntityA));
            entity.Property("Name");
            entity.Property("Name");

            Assert.Single(entity.GetAccessor().Metadata.GetProperties());
        }

        public class FakeEntityA
        {
            public int No { get; set; }

            public string Name { get; set; }

            public string Content { get; set; }

            public string fieldInfo;
        }
    }
}
