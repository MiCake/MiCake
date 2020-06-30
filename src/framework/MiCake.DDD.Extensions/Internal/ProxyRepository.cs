using MiCake.DDD.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.Internal
{
    internal class ProxyRepository<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        private IRepository<TAggregateRoot, TKey> _inner;

        public ProxyRepository(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IRepositoryFactory<TAggregateRoot, TKey>>() ??
                            throw new NullReferenceException($"Cannot get a {nameof(IRepositoryFactory<TAggregateRoot, TKey>)} instance.");

            _inner = factory.CreateRepository();
        }

        public void Add(TAggregateRoot aggregateRoot)
            => _inner.Add(aggregateRoot);

        public TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot, bool autoExecute)
            => _inner.AddAndReturn(aggregateRoot, autoExecute);

        public Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute, CancellationToken cancellationToken = default)
            => _inner.AddAndReturnAsync(aggregateRoot, autoExecute, cancellationToken);

        public Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
            => _inner.AddAsync(aggregateRoot, cancellationToken);

        public void Delete(TAggregateRoot aggregateRoot)
            => _inner.Delete(aggregateRoot);

        public Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
            => _inner.DeleteAsync(aggregateRoot, cancellationToken);

        public TAggregateRoot Find(TKey ID)
            => _inner.Find(ID);

        public Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
            => _inner.FindAsync(ID, cancellationToken);

        public long GetCount()
            => _inner.GetCount();

        public void Update(TAggregateRoot aggregateRoot)
            => _inner.Update(aggregateRoot);

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
            => _inner.UpdateAsync(aggregateRoot, cancellationToken);
    }
}
