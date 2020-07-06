using MiCake.DDD.Domain.Freedom;
using MiCake.DDD.Extensions.Internal;
using MiCake.DDD.Tests.Fakes.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace MiCake.DDD.Tests.ProxyRepository
{
    public class ProxyFreeRepository_Tests : DefaultFreeRepositoryFactory_Tests
    {
        public ProxyFreeRepository_Tests() : base()
        {
            Services.AddScoped(typeof(IFreeRepository<,>), typeof(ProxyFreeRepository<,>));
            Services.AddScoped(typeof(IReadOnlyFreeRepository<,>), typeof(ProxyReadOnlyFreeRepository<,>));
        }

        [Fact]
        public void ProxyFreeRepository_ShouldRightResult()
        {
            var provider = Services.BuildServiceProvider();

            var repository = provider.GetService<IFreeRepository<EntityA, int>>();

            Assert.NotNull(repository);

            //some api tests
            var entity = new EntityA() { Id = 1 };
            var entity2 = new EntityA() { Id = 2 };
            var entity3 = new EntityA() { Id = 3 };
            var entity4 = new EntityA() { Id = 4 };

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
            Assert.Equal(2, repository.GetCountAsync().Result);

            var list = repository.GetAll();
            var listAsync = repository.GetAllAsync().Result;
            Assert.Equal(2, list.Count());

            Assert.Equal(0, repository.FindMatch(s => s.Id == 0).Count());
        }

        [Fact]
        public void ProxyReadOnlyFreeRepository_ShouldRightResult()
        {
            var provider = Services.BuildServiceProvider();

            var repository = provider.GetService<IReadOnlyFreeRepository<EntityA, int>>();

            Assert.NotNull(repository);
            Assert.Null(repository.Find(1));
            Assert.Null(repository.FindAsync(1).Result);
            Assert.Equal(0, repository.GetCount());
            Assert.Equal(0, repository.GetCountAsync().Result);
            Assert.Empty(repository.GetAll());
            Assert.Empty(repository.GetAllAsync().Result);
        }
    }
}
