using MiCake.Cord.Tests;
using MiCake.Cord.Tests.Fakes.Aggregates;
using MiCake.Core;
using MiCake.DDD.Connector;
using MiCake.DDD.Connector.Internal;
using MiCake.DDD.Connector.Metadata;
using MiCake.DDD.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using Xunit;

namespace MiCake.Cord.Tests.ProxyRepository
{
    public class DefaultRepositoryFacotry_Tests : MiCakeDDDTestsBase
    {
        public DefaultRepositoryFacotry_Tests() : base()
        {
            //Add domainMetadata
            Assembly[] assemblies = { GetType().Assembly };
            Services.Configure<MiCakeApplicationOptions>(options => options.DomainLayerAssemblies = assemblies);
            BuildServiceCollection();

            Services.AddTransient(typeof(IRepositoryFactory<,>), typeof(DefaultRepositoryFacotry<,>));

            //Add Repository Provider
            Services.AddTransient(typeof(IRepositoryProvider<,>), typeof(TestRepositoryProvider<,>));
        }

        [Fact]
        public void GetRepository_WrongKeyType_Throw()
        {
            var provider = Services.BuildServiceProvider();

            Assert.ThrowsAny<ArgumentException>(() =>
            {
                //HasEventsAggregate right primary key is long
                var repositoryType = typeof(IRepositoryFactory<,>).MakeGenericType(typeof(HasEventsAggregate), typeof(Guid));
            });
        }

        [Fact]
        public void DefaultRepositoryFacotry_GetNotInMetadataType_Throw()
        {
            var provider = Services.BuildServiceProvider();
            var domainMetadata = provider.GetService<DomainMetadata>();

            domainMetadata.DomainObject.AggregateRoots.RemoveAll(s => true);

            var factory = provider.GetService<IRepositoryFactory<HasEventsAggregate, int>>();
            Assert.Throws<NullReferenceException>(() =>
            {
                factory.CreateRepository();
            });
        }

        [Fact]
        public void DefaultRepositoryFacotry_MoreProvider_ShouldUseLastOne()
        {
            //Add Null Instance Repository Provider
            Services.AddTransient(typeof(IRepositoryProvider<,>), typeof(NullInstanceRepositoryProvider<,>));

            var provider = Services.BuildServiceProvider();

            var factory = provider.GetService<IRepositoryFactory<HasEventsAggregate, int>>();
            var respository = factory.CreateRepository();

            Assert.Null(respository);
        }

        [Fact]
        public void DefaultRepositoryFacotry_ShouldGetRightRepository()
        {
            var provider = Services.BuildServiceProvider();

            var factory = provider.GetService<IRepositoryFactory<HasEventsAggregate, int>>();
            var respository = factory.CreateRepository();

            Assert.NotNull(respository);
            Assert.IsAssignableFrom<IRepository<HasEventsAggregate, int>>(respository);

            var readonlyRepository = factory.CreateReadOnlyRepository();
            Assert.NotNull(readonlyRepository);
            Assert.IsAssignableFrom<IReadOnlyRepository<HasEventsAggregate, int>>(respository);
        }
    }
}
