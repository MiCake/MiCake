using MiCake.Core.Data;
using MiCake.DDD.Extensions.Store.Mapping;
using MiCake.EntityFrameworkCore.Mapping;
using MiCake.EntityFrameworkCore.Tests.Fakes;
using MiCake.Mapster;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Mapping
{
    public class PoManager_UseMapster_Tests
    {
        public IPersistentObjectMapper MapsterMapper { get; set; } = new MapsterPersistentObjectMapper();

        public PoManager_UseMapster_Tests()
        {
            ActiveAllPOConfig();
        }

        [Fact]
        public void MapDoAndPO_NormalMap()
        {
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(MapsterMapper);

            var aggregateRoot = new PersistentAggregateRoot("Hello", new ColorValueObject(255, 255, 255));
            var poResult = mapper.MapToPO(aggregateRoot);

            Assert.NotNull(poResult);
            Assert.Equal("Hello", poResult.Name);
            Assert.Equal(255, poResult.R);
            Assert.Equal(255, poResult.G);
            Assert.Equal(255, poResult.B);

            var pOModel = new AggregateRootPOModel("Hello", 255, 255, 255);
            var doResult = mapper.MapToDO(pOModel);

            Assert.NotNull(doResult);
            Assert.Equal("Hello", doResult.Name);
            Assert.Equal(255, doResult.Color.R);
            Assert.Equal(255, doResult.Color.G);
            Assert.Equal(255, doResult.Color.B);
        }

        [Fact]
        public void MapDoAndPO_MapList()
        {
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(MapsterMapper);

            var aggregateRoots = new List<PersistentAggregateRoot>
            {
                new PersistentAggregateRoot("Hello", new ColorValueObject(255, 255, 255)),
                new PersistentAggregateRoot("World", new ColorValueObject(0, 0, 0))
            };

            var poResult = mapper.MapToPO(aggregateRoots);

            Assert.NotNull(poResult);
            Assert.Equal(2, poResult.Count());

            var helloResult = poResult.FirstOrDefault(s => s.Name.Equals("Hello"));
            Assert.NotNull(helloResult);
            Assert.Equal(255, helloResult.R);
            Assert.Equal(255, helloResult.G);
            Assert.Equal(255, helloResult.B);

            var poModels = new List<AggregateRootPOModel>
            {
                new AggregateRootPOModel("Hello", 255,255,255),
                new AggregateRootPOModel("World", 0,0,0)
            };

            var doResult = mapper.MapToDO(poModels);

            Assert.NotNull(doResult);
            Assert.Equal(2, doResult.Count());

            var helloDoResult = doResult.FirstOrDefault(s => s.Name.Equals("Hello"));
            Assert.NotNull(helloDoResult);
            Assert.Equal(255, helloDoResult.Color.R);
            Assert.Equal(255, helloDoResult.Color.G);
            Assert.Equal(255, helloDoResult.Color.B);

        }

        [Fact]
        public void MapDoAndPO_HasDomainEvent()
        {
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(MapsterMapper);

            var aggregateRoot = new PersistentAggregateRoot("Hello", new ColorValueObject(255, 255, 255));
            aggregateRoot.AddDomainEvent(new DemoDomainEvent() { Name = "Event" });
            var poResult = mapper.MapToPO(aggregateRoot);

            var @events = poResult.GetDomainEvents();
            Assert.Single(@events);
            Assert.Equal("Event", ((DemoDomainEvent)@events.First()).Name);
        }

        [Fact]
        public void MapDoAndPO_GiveOriginalObject_ShouldChangeOriginalObjectValue()
        {
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(MapsterMapper);

            var poSource = new AggregateRootPOModel("Hello", 255, 255, 255);
            //For example:repository.Find();
            var doResult = mapper.MapToDO(poSource);

            doResult.Id = 10086;
            //For example:repository.Update();
            var updatedPoSource = mapper.MapToPO(doResult);

            Assert.Same(poSource, updatedPoSource);
            Assert.Equal(10086, updatedPoSource.Id);
        }

        [Fact]
        public void MapDoAndPO_ShouldDispose()
        {
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(MapsterMapper);

            var aggregateRoot = new PersistentAggregateRoot("Hello", new ColorValueObject(255, 255, 255));
            var poResult = mapper.MapToPO(aggregateRoot);
        }

        private void ActiveAllPOConfig()
        {
            //use to active mapping info.
            AggregateRootPOModel po = new AggregateRootPOModel();
            (po as INeedParts<IPersistentObjectMapper>).SetParts(MapsterMapper);

            po.ConfigureMapping();
        }
    }
}
