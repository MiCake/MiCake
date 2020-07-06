using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using Microsoft.Extensions.DependencyInjection;
using System;
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

            _inner = factory.CreateReadOnlyFreeRepository();
        }

        public TEntity Find(TKey ID)
            => _inner.Find(ID);

        public Task<TEntity> FindAsync(TKey ID, CancellationToken cancellationToken = default)
            => _inner.FindAsync(ID, cancellationToken);

        public IQueryable<TEntity> FindMatch(Expression<Func<TEntity, bool>> propertySelectors)
            => _inner.FindMatch(propertySelectors);

        public long GetCount()
            => _inner.GetCount();

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
            => _inner.GetCountAsync(cancellationToken);

        public IQueryable<TEntity> GetAll()
            => _inner.GetAll();

        public Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
            => _inner.GetAllAsync(cancellationToken);

        public Task<IQueryable<TEntity>> FindMatchAsync(Expression<Func<TEntity, bool>> propertySelectors, CancellationToken cancellationToken = default)
            => _inner.FindMatchAsync(propertySelectors, cancellationToken);
    }
}
