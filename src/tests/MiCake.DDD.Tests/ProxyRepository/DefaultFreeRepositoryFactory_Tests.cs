using MiCake.Core;
using MiCake.DDD.Domain.Freedom;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Internal;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Tests.Fakes.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using Xunit;

namespace MiCake.DDD.Tests.ProxyRepository
{
    public class DefaultFreeRepositoryFactory_Tests : MiCakeDDDTestsBase
    {
        public DefaultFreeRepositoryFactory_Tests() : base()
        {
            //Add domainMetadata
            Assembly[] assemblies = { GetType().Assembly };
            Services.Configure<MiCakeApplicationOptions>(options => options.DomianLayerAssemblies = assemblies);
            BuildServiceCollection();

            Services.AddTransient(typeof(IFreeRepositoryFactory<,>), typeof(DefaultFreeRepositoryFactory<,>));

            //Add Repository Provider
            Services.AddTransient(typeof(IFreeRepositoryProvider<,>), typeof(TestFreeRepositoryProvider<,>));
        }

        [Fact]
        public void GetFreeRepository_WrongKeyType_Throw()
        {
            var provider = Services.BuildServiceProvider();

            Assert.ThrowsAny<ArgumentException>(() =>
            {
                //HasEventsAggregate right primary key is long
                var repositoryType = typeof(IFreeRepositoryFactory<,>).MakeGenericType(typeof(HasEventsAggregate), typeof(Guid));
            });
        }

        [Fact]
        public void DefaultFreeRepositoryFacotry_GetNotInMetadataType_Throw()
        {
            var provider = Services.BuildServiceProvider();
            var domainMetadata = provider.GetService<DomainMetadata>();

            domainMetadata.DomainObject.Entities.RemoveAll(s => true);

            var factory = provider.GetService<IFreeRepositoryFactory<HasEventsAggregate, int>>();
            Assert.Throws<NullReferenceException>(() =>
            {
                factory.CreateFreeRepository();
            });
        }

        [Fact]
        public void DefaultFreeRepositoryFacotry_MoreProvider_ShouldUseLastOne()
        {
            //Add Null Instance Repository Provider
            Services.AddTransient(typeof(IFreeRepositoryProvider<,>), typeof(NullInstanceFreeRepositoryProvider<,>));

            var provider = Services.BuildServiceProvider();

            var factory = provider.GetService<IFreeRepositoryFactory<HasEventsAggregate, int>>();
            var respository = factory.CreateFreeRepository();

            Assert.Null(respository);
        }

        [Fact]
        public void DefaultFreeRepositoryFacotry_ShouldGetRightRepository()
        {
            var provider = Services.BuildServiceProvider();
            var factory = provider.GetService<IFreeRepositoryFactory<HasEventsAggregate, int>>();
            var respository = factory.CreateFreeRepository();

            Assert.NotNull(respository);
            Assert.IsAssignableFrom<IFreeRepository<HasEventsAggregate, int>>(respository);

            var readOnlyRespository = factory.CreateReadOnlyFreeRepository();

            Assert.NotNull(readOnlyRespository);
            Assert.IsAssignableFrom<IReadOnlyFreeRepository<HasEventsAggregate, int>>(readOnlyRespository);
        }
    }
}
