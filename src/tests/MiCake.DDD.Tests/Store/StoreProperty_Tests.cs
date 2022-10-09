using MiCake.Cord.Storage.Internal;
using Xunit;

namespace MiCake.Cord.Tests.Store
{
    public class StoreProperty_Tests : StoreConfigTestBase
    {
        public StoreProperty_Tests()
        {
        }

        [Fact]
        public void Model_Add_Property_Is_StoreProperty()
        {
            var model = CreateModel();

            var entityType = (StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA));

            Assert.IsType<StoreProperty>(entityType.AddProperty("Name"));
        }

        [Fact]
        public void Can_Set_ClrPropertyType()
        {
            var model = CreateModel();

            var entityProperty = ((StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA))).AddProperty("Name");

            Assert.Equal("Name", entityProperty.Name);
            Assert.NotNull(entityProperty.ClrPropertyInfo);
        }

        [Fact]
        public void Can_Set_StoreEntityType()
        {
            var model = CreateModel();

            var entityProperty = ((StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA))).AddProperty("Name");

            Assert.NotNull(entityProperty.StoreEntityType);
            Assert.Equal("FakeEntityA", entityProperty.StoreEntityType.Name);
        }

        [Fact]
        public void Can_Set_Concurrency()
        {
            var model = CreateModel();

            var property = ((StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA))).AddProperty("Name");
            property.SetConcurrency(true);

            Assert.True(property.IsConcurrency);
        }

        [Fact]
        public void Can_Set_DefaultValue()
        {
            var model = CreateModel();

            var property = ((StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA))).AddProperty("Name");
            property.SetDefaultValue(3);

            Assert.Equal(3, property.DefaultValue.Value.DefaultValue);
        }

        [Fact]
        public void Can_Set_MaxLength()
        {
            var model = CreateModel();

            var property = ((StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA))).AddProperty("Name");
            property.SetMaxLength(100);

            Assert.Equal(100, property.MaxLength);
        }

        [Fact]
        public void Can_Set_Nullable()
        {
            var model = CreateModel();

            var property = ((StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA))).AddProperty("Name");
            property.SetNullable(true);

            Assert.True(property.IsNullable);
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
