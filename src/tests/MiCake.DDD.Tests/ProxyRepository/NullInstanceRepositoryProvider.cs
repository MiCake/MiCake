using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using MiCake.DDD.Extensions;

namespace MiCake.DDD.Tests.ProxyRepository
{
    public class NullInstanceRepositoryProvider<TAggregateRoot, TKey> : IRepositoryProvider<TAggregateRoot, TKey>
      where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        public IReadOnlyRepository<TAggregateRoot, TKey> GetReadOnlyRepository()
        {
            return null;
        }

        public IRepository<TAggregateRoot, TKey> GetRepository()
        {
            return null;
        }
    }

    public class NullInstanceFreeRepositoryProvider<TAggregateRoot, TKey> : IFreeRepositoryProvider<TAggregateRoot, TKey>
      where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        public IFreeRepository<TAggregateRoot, TKey> GetFreeRepository()
        {
            return null;
        }

        public IReadOnlyFreeRepository<TAggregateRoot, TKey> GetReadOnlyFreeRepository()
        {
            return null;
        }
    }
}
