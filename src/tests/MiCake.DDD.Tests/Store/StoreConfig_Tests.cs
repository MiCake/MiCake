using MiCake.Cord.Storage;
using MiCake.Cord.Storage.Internal;
using System.Linq;
using Xunit;

namespace MiCake.Cord.Tests.Store
{
    public class StoreConfig_Tests : StoreConfigTestBase
    {
        public StoreConfig_Tests()
        {
        }

        [Fact]
        public void Create_Model_With_OneProvider()
        {
            var config = new StoreConfig().AddModelProvider(new FakeStoreModelProvider());
            var model = config.GetStoreModel();
            Assert.Single(model.GetStoreEntities());

            var entityType = model.GetStoreEntities().First();
            Assert.Single(entityType.GetQueryFilters());
            Assert.Equal("FakeEntityA", ((StoreEntityType)entityType).Name);
        }

        [Fact]
        public void Create_Model_With_TwoProvider_ConfigSameEntity()
        {
            var config = new StoreConfig();
            config.AddModelProvider(new FakeStoreModelProvider());
            config.AddModelProvider(new FakeStoreModelProvider2());

            var model = config.GetStoreModel();
            Assert.Single(model.GetStoreEntities());

            var entityType = model.GetStoreEntities().First();
            Assert.Single(entityType.GetQueryFilters());
            Assert.Single(entityType.GetIgnoredMembers());
        }

        [Fact]
        public void Create_Model_With_TwoProvider_ConfigDifferentEntity()
        {
            var config = new StoreConfig();
            config.AddModelProvider(new FakeStoreModelProvider());
            config.AddModelProvider(new FakeStoreModelProvider3());

            var model = config.GetStoreModel();
            Assert.Equal(2, model.GetStoreEntities().Count());

            var entityType = model.FindStoreEntity(typeof(FakeEntityA));
            Assert.NotNull(entityType);
            Assert.Single(entityType.GetQueryFilters());

            var entityTypeB = model.FindStoreEntity(typeof(FakeEntityB));
            Assert.NotNull(entityTypeB);
            Assert.Single(entityTypeB.GetIgnoredMembers());
        }

        [Fact]
        public void Can_Remove_StoreEntity()
        {
            var config = new StoreConfig();
            config.AddModelProvider(new FakeStoreModelProvider());
            var model = config.GetStoreModel();

            Assert.Single(model.GetStoreEntities());

            model.RemoveStoreEntity(typeof(FakeEntityA));
            Assert.Empty(model.GetStoreEntities());
        }

        public class FakeStoreModelProvider : IStoreModelProvider
        {
            public void Config(StoreModelBuilder modelBuilder)
            {
                modelBuilder.Entity<FakeEntityA>().HasQueryFilter(s => s.Name.Equals("AAA"));
            }
        }

        public class FakeStoreModelProvider2 : IStoreModelProvider
        {
            public void Config(StoreModelBuilder modelBuilder)
            {
                modelBuilder.Entity<FakeEntityA>().Ignored("No");
            }
        }

        public class FakeStoreModelProvider3 : IStoreModelProvider
        {
            public void Config(StoreModelBuilder modelBuilder)
            {
                modelBuilder.Entity<FakeEntityB>().Ignored("No");
            }
        }

        public class FakeEntityA
        {
            public int No { get; set; }

            public string Name { get; set; }

            public string Content { get; set; }

            public string fieldInfo;
        }

        public class FakeEntityB
        {
            public int No { get; set; }

            public string Name { get; set; }

            public string Content { get; set; }

            public string fieldInfo;
        }
    }
}
