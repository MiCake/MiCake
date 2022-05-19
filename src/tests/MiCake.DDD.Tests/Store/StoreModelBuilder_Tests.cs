using MiCake.Core.Data;
using System;
using System.Linq.Expressions;
using Xunit;

namespace MiCake.Cord.Tests.Store
{
    public class StoreModelBuilder_Tests : StoreConfigTestBase
    {
        public StoreModelBuilder_Tests()
        {
        }

        [Fact]
        public void Can_Set_Entity_Rule_WithGeneric()
        {
            var builder = CreateStoreModelBuilder();
            var entity = builder.Entity<FakeEntityA>();

            entity.DirectDeletion(false);
            entity.HasQueryFilter(s => s.Name.Equals("A"));
            entity.Ignored("No");
        }

        [Fact]
        public void Can_Set_Entity_Rule()
        {
            var builder = CreateStoreModelBuilder();
            var entity = builder.Entity(typeof(FakeEntityA));
            Expression<Func<FakeEntityA, bool>> lambdaExpress = s => s.Name.Equals("A");

            entity.DirectDeletion(false);
            entity.HasQueryFilter(lambdaExpress);
            entity.Ignored("No");
        }

        [Fact]
        public void Can_Add_Entity()
        {
            var builder = CreateStoreModelBuilder();
            var entity = builder.Entity<FakeEntityA>();

            Assert.NotNull(entity.GetAccessor().Name);

            var builder2 = CreateStoreModelBuilder();
            var entity2 = builder2.Entity(typeof(FakeEntityA));

            Assert.NotNull(entity2.GetAccessor().Name);
        }

        [Fact]
        public void Can_Add_Same_Entity_But_Only_OneType()
        {
            var builder = CreateStoreModelBuilder();
            builder.Entity<FakeEntityA>();
            builder.Entity<FakeEntityA>();
            builder.Entity(typeof(FakeEntityA));

            Assert.Single(builder.GetAccessor().GetStoreEntities());
        }

        [Fact]
        public void Can_Add_Property()
        {
            var builder = CreateStoreModelBuilder();
            var propertyBuilder = builder.Entity<FakeEntityA>().Property(s => s.Content);

            Assert.NotNull(propertyBuilder.GetAccessor().Name);
            Assert.Equal("Content", propertyBuilder.GetAccessor().Name);
        }

        [Fact]
        public void Cannot_Add_FieldInfo()
        {
            var builder = CreateStoreModelBuilder();
            Assert.Throws<ArgumentException>(() => builder.Entity<FakeEntityA>().Property(s => s.fieldInfo));
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
