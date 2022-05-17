using MiCake.DDD.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Cord.Tests.ProxyRepository
{
    public class TestReadOnlyRepository<TAggregateRoot, TKey> : IReadOnlyRepository<TAggregateRoot, TKey>
          where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        protected List<TAggregateRoot> Data { get; set; }

        public TestReadOnlyRepository()
        {
            Data = new List<TAggregateRoot>();
        }

        public TAggregateRoot Find(TKey ID)
        => Data.FirstOrDefault(s => s.Id.Equals(ID));

        public Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var result = Data.FirstOrDefault(s => s.Id.Equals(ID));
            return Task.FromResult(result);
        }

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((long)Data.Count);
        }
    }
}
