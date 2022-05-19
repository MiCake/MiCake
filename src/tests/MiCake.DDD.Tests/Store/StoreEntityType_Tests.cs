using MiCake.Cord.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace MiCake.Cord.Tests.Store
{
    public class StoreEntityType_Tests : StoreConfigTestBase
    {
        public StoreEntityType_Tests()
        {
        }

        [Fact]
        public void Model_Add_Type_Is_StoreEntity()
        {
            var model = CreateModel();

            var entityType = model.AddStoreEntity(typeof(FakeOrder));
            Assert.IsType<StoreEntityType>(entityType);
        }

        [Fact]
        public void Can_Set_ClrType()
        {
            var model = CreateModel();

            var entityType = model.AddStoreEntity(typeof(FakeOrder));
            Assert.Equal(typeof(FakeOrder), ((StoreEntityType)entityType).ClrType);
            Assert.Equal(typeof(FakeOrder).Name, ((StoreEntityType)entityType).Name);
        }

        [Fact]
        public void Can_Set_DirectDeletion()
        {
            var model = CreateModel();

            var entityType = model.AddStoreEntity(typeof(FakeOrder));
            entityType.SetDirectDeletion(false);

            Assert.False(entityType.DirectDeletion);
        }

        [Fact]
        public void Can_Add_IgnoredMember()
        {
            var model = CreateModel();

            var entityType = model.AddStoreEntity(typeof(FakeOrder));
            entityType.AddIgnoredMember("OrderName");

            Assert.Single(entityType.GetIgnoredMembers());
            Assert.Equal("OrderName", entityType.GetIgnoredMembers().FirstOrDefault());
        }

        [Fact]
        public void Can_Add_Same_IgnoredMember_But_Not_Double_Result()
        {
            var model = CreateModel();

            var entityType = model.AddStoreEntity(typeof(FakeOrder));
            entityType.AddIgnoredMember("OrderName");
            //Add Double
            entityType.AddIgnoredMember("OrderName");

            Assert.Single(entityType.GetIgnoredMembers());
            Assert.Equal("OrderName", entityType.GetIgnoredMembers().FirstOrDefault());
        }

        [Fact]
        public void Can_Add_QueryFilter()
        {
            var model = CreateModel();

            Expression<Func<FakeOrder, bool>> expression = order => order.OrderName.Equals("s");
            var entityType = model.AddStoreEntity(typeof(FakeOrder));
            entityType.AddQueryFilter(expression);

            Assert.Single(entityType.GetQueryFilters());
        }

        [Fact]
        public void Can_Add_Property()
        {
            var model = CreateModel();

            var entityType = model.AddStoreEntity(typeof(FakeEntityA));
            entityType.AddProperty("Name", typeof(FakeEntityA).GetProperty("Name"));

            Assert.Single(entityType.GetProperties());
            Assert.NotNull(entityType.FindProperty("Name"));
        }

        [Fact]
        public void Can_Add_Property_With_No_Member()
        {
            var model = CreateModel();

            var entityType = (StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA));
            entityType.AddProperty("Name");

            Assert.Single(entityType.GetProperties());
        }

        [Fact]
        public void Cannot_Add_Property_With_No_Belong_Name()
        {
            var model = CreateModel();

            var entityType = (StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA));
            Assert.Throws<InvalidOperationException>(() =>
            {
                entityType.AddProperty("NameXXX");
            });
        }

        [Fact]
        public void Cannot_Add_Property_With_field()
        {
            var model = CreateModel();

            var entityType = (StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA));
            Assert.Throws<InvalidOperationException>(() =>
            {
                entityType.AddProperty("fieldInfo");
            });
        }

        [Fact]
        public void Can_Add_Property_With_InHierarchy_Property()
        {
            var model = CreateModel();

            var entityType = (StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA));
            entityType.AddProperty("BaseName");

            Assert.Single(entityType.GetProperties());
        }

        [Fact]
        public void Cannot_Add_Property_With_WrongCase()
        {
            var model = CreateModel();

            var entityType = (StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA));
            Assert.Throws<InvalidOperationException>(() =>
            {
                entityType.AddProperty("name");
            });
        }

        [Fact]
        public void Can_Find_Property()
        {
            var model = CreateModel();

            var entityType = (StoreEntityType)model.AddStoreEntity(typeof(FakeEntityA));
            entityType.AddProperty("Name");

            Assert.NotNull(entityType.FindProperty("Name"));
        }

        public class FakeEntityBase
        {
            public string BaseName { get; set; }
        }

        public class FakeEntityA : FakeEntityBase
        {
            public int No { get; set; }

            public string Name { get; set; }

            public string Content { get; set; }

            public string fieldInfo;

            public List<FakeOrder> Orders { get; set; }
        }

        public class FakeOrder
        {
            public int ID { get; set; }

            public string OrderName { get; set; }
        }
    }
}
