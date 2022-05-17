using MiCake.Core.Data;
using System;
using System.Linq.Expressions;
using Xunit;

namespace MiCake.Cord.Tests.Store
{
    public class InternalStoreEntityBuilder_Tests : StoreConfigTestBase
    {
        public InternalStoreEntityBuilder_Tests()
        {
        }

        [Fact]
        public void Can_Set_InternalStoreEntityBuilder()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).GetAccessor();
            Assert.NotNull(builder);
        }

        [Fact]
        public void Can_Set_DirectDeletion()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).GetAccessor();
            builder.SetDirectDeletion(true);
            Assert.True(builder.Metadata.DirectDeletion);
        }

        [Fact]
        public void Can_Add_IgnoredMember()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).GetAccessor();
            builder.AddIgnoredMember("Name");
            Assert.Single(builder.Metadata.GetIgnoredMembers());
        }

        [Fact]
        public void Cannot_Add_IgnoredMember_with_wrongcase_propertyname()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).GetAccessor();
            Assert.Throws<ArgumentException>(() => builder.AddIgnoredMember("name"));
        }

        [Fact]
        public void Cannot_Add_IgnoredMember_with_null()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).GetAccessor();
            Assert.Throws<ArgumentException>(() => builder.AddIgnoredMember(null));
        }

        [Fact]
        public void Cannot_Add_QueryFilter()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var builder = modelbuilder.Entity(typeof(FakeEntityA)).GetAccessor();
            Expression<Func<FakeEntityA, bool>> expression = entity => entity.Name.Equals("s");
            builder.AddQueryFilter(expression);

            Assert.Single(builder.Metadata.GetQueryFilters());
        }

        [Fact]
        public void Can_Add_Property()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var entityBuilder = modelbuilder.Entity(typeof(FakeEntityA));
            entityBuilder.Property("Name");

            Assert.Single(entityBuilder.GetAccessor().Metadata.GetProperties());
        }

        [Fact]
        public void Cannot_Add_Property_With_Not_Belong_Name()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var entityBuilder = modelbuilder.Entity(typeof(FakeEntityA));
            Assert.Throws<InvalidOperationException>(() => entityBuilder.Property("Namexxx", typeof(string)));
        }

        [Fact]
        public void Can_Add_Property_With_Same_Name_ButOnlyOne()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var entityBuilder = modelbuilder.Entity(typeof(FakeEntityA));
            entityBuilder.Property("Name");
            entityBuilder.Property("Name", typeof(string));

            Assert.Single(entityBuilder.GetAccessor().Metadata.GetProperties());
        }

        [Fact]
        public void Cannot_Add_Property_With_Field()
        {
            var modelbuilder = CreateStoreModelBuilder();

            var entityBuilder = modelbuilder.Entity(typeof(FakeEntityA));
            Assert.Throws<InvalidOperationException>(() => entityBuilder.Property("fieldInfo"));
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
