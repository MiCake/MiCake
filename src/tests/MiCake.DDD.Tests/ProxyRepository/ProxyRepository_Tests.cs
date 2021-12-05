using MiCake.DDD.Connector.Internal;
using MiCake.DDD.Domain;
using MiCake.DDD.Tests.Fakes.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.DDD.Tests.ProxyRepository
{
    public class ProxyRepository_Tests : DefaultRepositoryFacotry_Tests
    {
        public ProxyRepository_Tests() : base()
        {
            Services.AddScoped(typeof(IRepository<,>), typeof(ProxyRepository<,>));
            Services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(ProxyReadOnlyRepository<,>));
        }

        [Fact]
        public async Task ProxyRepository_ShouldRightResult()
        {
            var provider = Services.BuildServiceProvider();

            var repository = provider.GetService<IRepository<HasEventsAggregate, int>>();

            Assert.NotNull(repository);

            //some api tests
            var entity = new HasEventsAggregate() { Id = 1 };
            var entity2 = new HasEventsAggregate() { Id = 2 };
            var entity3 = new HasEventsAggregate() { Id = 3 };
            var entity4 = new HasEventsAggregate() { Id = 4 };

            await repository.AddAsync(entity2);
            Assert.Equal(1, await repository.GetCountAsync());

            await repository.AddAndReturnAsync(entity4);
            Assert.Equal(2, await repository.GetCountAsync());

            await repository.DeleteAsync(entity2);
            Assert.Equal(1, await repository.GetCountAsync());

            Assert.NotNull(repository.FindAsync(4).Result);

            await repository.UpdateAsync(entity4);
            Assert.Equal(1, await repository.GetCountAsync());
        }

        [Fact]
        public async Task ProxyReadOnlyRepository_ShouldRightResult()
        {
            var provider = Services.BuildServiceProvider();

            var repository = provider.GetService<IReadOnlyRepository<HasEventsAggregate, int>>();

            Assert.NotNull(repository);
            Assert.Null(await repository.FindAsync(1));
            Assert.Null(await repository.FindAsync(1));
            Assert.Equal(0, await repository.GetCountAsync());
        }
    }
}
