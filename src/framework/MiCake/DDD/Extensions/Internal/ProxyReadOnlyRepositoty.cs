using MiCake.DDD.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.Internal
{
    internal class ProxyReadOnlyRepository<TAggregateRoot, TKey> : IReadOnlyRepository<TAggregateRoot, TKey>
          where TAggregateRoot : class, IAggregateRoot<TKey>
          where TKey : notnull
    {
        private readonly IReadOnlyRepository<TAggregateRoot, TKey> _inner;

        public ProxyReadOnlyRepository(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IRepositoryFactory<TAggregateRoot, TKey>>() ??
                            throw new NullReferenceException($"Cannot get a {nameof(IRepositoryFactory<TAggregateRoot, TKey>)} instance.");

            _inner = factory.CreateReadOnlyRepository();
        }

        public IQueryable<TAggregateRoot> Query()
            => _inner.Query();

        public Task<TAggregateRoot?> FindAsync(TKey id, CancellationToken cancellationToken = default)
            => _inner.FindAsync(id, cancellationToken);

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
            => _inner.GetCountAsync(cancellationToken);
    }
}
