using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Internal;
using MiCake.DDD.Tests.Fakes.Aggregates;
using Microsoft.Extensions.DependencyInjection;
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
        public void ProxyRepository_ShouldRightResult()
        {
            var provider = Services.BuildServiceProvider();

            var repository = provider.GetService<IRepository<HasEventsAggregate, int>>();

            Assert.NotNull(repository);

            //some api tests
            var entity = new HasEventsAggregate() { Id = 1 };
            var entity2 = new HasEventsAggregate() { Id = 2 };
            var entity3 = new HasEventsAggregate() { Id = 3 };
            var entity4 = new HasEventsAggregate() { Id = 4 };

            repository.Add(entity);
            repository.AddAsync(entity2);
            Assert.Equal(2, repository.GetCount());

            repository.AddAndReturn(entity3);
            repository.AddAndReturnAsync(entity4);
            Assert.Equal(4, repository.GetCount());

            repository.Delete(entity);
            repository.DeleteAsync(entity2);
            Assert.Equal(2, repository.GetCount());

            Assert.NotNull(repository.Find(3));
            Assert.NotNull(repository.FindAsync(4).Result);

            repository.Update(entity3);
            repository.UpdateAsync(entity4);
            Assert.Equal(2, repository.GetCount());
        }

        [Fact]
        public void ProxyReadOnlyRepository_ShouldRightResult()
        {
            var provider = Services.BuildServiceProvider();

            var repository = provider.GetService<IReadOnlyRepository<HasEventsAggregate, int>>();

            Assert.NotNull(repository);
            Assert.Null(repository.Find(1));
            Assert.Null(repository.FindAsync(1).Result);
            Assert.Equal(0, repository.GetCount());
        }
    }
}
