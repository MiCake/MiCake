using MiCake.DDD.Domain;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Extensions.Internal
{
    internal class ProxyReadOnlyRepository<TAggregateRoot, TKey> : IReadOnlyRepository<TAggregateRoot, TKey>
          where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        private IReadOnlyRepository<TAggregateRoot, TKey> _inner;

        public ProxyReadOnlyRepository(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IRepositoryFactory<TAggregateRoot, TKey>>() ??
                            throw new NullReferenceException($"Cannot get a {nameof(IRepositoryFactory<TAggregateRoot, TKey>)} instance.");

            _inner = factory.CreateReadOnlyRepository();
        }

        public TAggregateRoot Find(TKey ID)
            => _inner.Find(ID);

        public Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
            => _inner.FindAsync(ID, cancellationToken);

        public long GetCount()
            => _inner.GetCount();
    }
}
