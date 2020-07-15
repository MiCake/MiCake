using AutoMapper;
using MiCake.AutoMapper;
using MiCake.Core.Data;
using MiCake.DDD.Extensions.Store.Mapping;
using MiCake.EntityFrameworkCore.Mapping;
using MiCake.EntityFrameworkCore.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Mapping
{
    public class PoManager_UseAutoMapper_Tests
    {
        public IPersistentObjectMapper AutoMapper_POMapper { get; set; }

        public PoManager_UseAutoMapper_Tests()
        {
            ActiveAllPOConfig();
        }

        [Fact]
        public void MapDoAndPO_NormalMap()
        {
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(AutoMapper_POMapper);

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
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(AutoMapper_POMapper);

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
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(AutoMapper_POMapper);

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
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(AutoMapper_POMapper);

            var poSource = new AggregateRootPOModel("Hello", 255, 255, 255) { Id = 10086 };
            //For example:repository.Find();
            var doResult = mapper.MapToDO(poSource);

            doResult.SetColor(new ColorValueObject(0, 0, 0));

            //For example:repository.Update();
            var updatedPoSource = mapper.MapToPO(doResult);

            Assert.Same(poSource, updatedPoSource);
            Assert.Equal(10086, updatedPoSource.Id);
            Assert.Equal(0, updatedPoSource.R);
            Assert.Equal(0, updatedPoSource.G);
            Assert.Equal(0, updatedPoSource.B);
        }

        [Fact]
        public void MapDoAndPO_GiveOriginalObject_ShouldChangeOriginalObjectValue_List()
        {
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(AutoMapper_POMapper);

            //List update
            var poModels = new List<AggregateRootPOModel>
            {
                new AggregateRootPOModel("Hello", 255,255,255){ Id = 1},
                new AggregateRootPOModel("World", 0,0,0){ Id=2}
            };

            var before = poModels.FirstOrDefault();

            var doResults = mapper.MapToDO(poModels).ToList();

            doResults.First().SetColor(new ColorValueObject(10, 10, 10));

            var updatedPoSources = mapper.MapToPO(doResults);

            var after = updatedPoSources.FirstOrDefault();

            Assert.Same(before, after);
            Assert.Equal(10, after.G);
        }

        [Fact]
        public void MapDoAndPO_ShouldDispose()
        {
            var mapper = new EFCorePoManager<PersistentAggregateRoot, AggregateRootPOModel>(AutoMapper_POMapper);

            var aggregateRoot = new PersistentAggregateRoot("Hello", new ColorValueObject(255, 255, 255));
            var poResult = mapper.MapToPO(aggregateRoot);

            mapper.Dispose();

            Assert.Throws<NullReferenceException>(() =>
            {
                mapper.MapToPO(aggregateRoot);
            });
        }

        private void ActiveAllPOConfig()
        {
            AutoMapper_POMapper = new AutoMapperPersistentObjectMapper(null);

            var config = new MapperConfiguration(cfg =>
             {
                 cfg.DisableConstructorMapping();

                 ((AutoMapperPersistentObjectMapper)AutoMapper_POMapper).SetAutoMapperConfigExpression(cfg);

                 //use to active mapping info.
                 AggregateRootPOModel po = new AggregateRootPOModel();
                 (po as INeedParts<IPersistentObjectMapper>).SetParts(AutoMapper_POMapper);
                 po.ConfigureMapping();
                 (po as IHasAccessor<IPersistentObjectMapConfig>)?.Instance?.Build();
             });

            var mapper = config.CreateMapper();
            AutoMapper_POMapper = new AutoMapperPersistentObjectMapper(mapper);
        }
    }
}