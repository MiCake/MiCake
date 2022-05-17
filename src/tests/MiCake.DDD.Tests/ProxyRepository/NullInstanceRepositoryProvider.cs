using MiCake.DDD.Connector;
using MiCake.DDD.Domain;

namespace MiCake.Cord.Tests.ProxyRepository
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
}
