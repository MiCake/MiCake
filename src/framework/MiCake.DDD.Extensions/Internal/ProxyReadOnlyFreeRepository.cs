using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Extensions.Internal
{
    internal class ProxyReadOnlyFreeRepository<TEntity, TKey> : IReadOnlyFreeRepository<TEntity, TKey>
          where TEntity : class, IEntity<TKey>
    {
        private IReadOnlyFreeRepository<TEntity, TKey> _inner;

        public ProxyReadOnlyFreeRepository(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IFreeRepositoryFactory<TEntity, TKey>>() ??
                            throw new NullReferenceException($"Cannot get a {nameof(IFreeRepositoryFactory<TEntity, TKey>)} instance.");

            _inner = factory.CreateFreeRepository();
        }

        public TEntity Find(TKey ID)
            => _inner.Find(ID);

        public Task<TEntity> FindAsync(TKey ID, CancellationToken cancellationToken = default)
            => _inner.FindAsync(ID, cancellationToken);

        public IQueryable<TEntity> FindMatch(params Expression<Func<TEntity, object>>[] propertySelectors)
            => _inner.FindMatch(propertySelectors);

        public long GetCount()
            => _inner.GetCount();

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
            => _inner.GetCountAsync(cancellationToken);

        public List<TEntity> GetList()
            => _inner.GetList();

        public Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
            => _inner.GetListAsync(cancellationToken);
    }
}
