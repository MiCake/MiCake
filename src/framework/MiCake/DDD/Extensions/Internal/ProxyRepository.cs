using MiCake.DDD.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.Internal
{
    internal class ProxyRepository<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
         where TKey : notnull
    {
        private readonly IRepository<TAggregateRoot, TKey> _inner;

        public ProxyRepository(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IRepositoryFactory<TAggregateRoot, TKey>>() ??
                            throw new NullReferenceException($"Cannot get a {nameof(IRepositoryFactory<TAggregateRoot, TKey>)} instance.");

            _inner = factory.CreateRepository();
        }

        public IQueryable<TAggregateRoot> Query()
            => _inner.Query();

        public Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute, CancellationToken cancellationToken = default)
            => _inner.AddAndReturnAsync(aggregateRoot, autoExecute, cancellationToken);

        public Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
            => _inner.AddAsync(aggregateRoot, cancellationToken);

        public Task ClearChangeTrackingAsync(CancellationToken cancellationToken = default)
            => _inner.ClearChangeTrackingAsync(cancellationToken);

        public Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
            => _inner.DeleteAsync(aggregateRoot, cancellationToken);

        public Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
         => _inner.DeleteByIdAsync(id, cancellationToken);

        public Task<TAggregateRoot?> FindAsync(TKey id, CancellationToken cancellationToken = default)
            => _inner.FindAsync(id, cancellationToken);

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
            => _inner.GetCountAsync(cancellationToken);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _inner.SaveChangesAsync(cancellationToken);
        }

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
            => _inner.UpdateAsync(aggregateRoot, cancellationToken);
    }
}
