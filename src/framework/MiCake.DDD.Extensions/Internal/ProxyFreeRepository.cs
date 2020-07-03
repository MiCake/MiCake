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
    internal class ProxyFreeRepository<TEntity, TKey> : IFreeRepository<TEntity, TKey>
         where TEntity : class, IEntity<TKey>
    {
        private IFreeRepository<TEntity, TKey> _inner;

        public ProxyFreeRepository(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IFreeRepositoryFactory<TEntity, TKey>>() ??
                            throw new NullReferenceException($"Cannot get a {nameof(IFreeRepositoryFactory<TEntity, TKey>)} instance.");

            _inner = factory.CreateFreeRepository();
        }

        public void Add(TEntity entity)
            => _inner.Add(entity);

        public TEntity AddAndReturn(TEntity entity, bool autoExecute = true)
            => _inner.AddAndReturn(entity, autoExecute);

        public Task<TEntity> AddAndReturnAsync(TEntity entity, bool autoExecute = true, CancellationToken cancellationToken = default)
            => _inner.AddAndReturnAsync(entity, autoExecute, cancellationToken);

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
            => _inner.AddAsync(entity, cancellationToken);

        public void Delete(TEntity entity)
            => _inner.Delete(entity);

        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
            => _inner.DeleteAsync(entity, cancellationToken);

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

        public void Update(TEntity entity)
            => _inner.Update(entity);

        public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
            => _inner.UpdateAsync(entity, cancellationToken);

        public Task<IQueryable<TEntity>> FindMatchAsync(Expression<Func<TEntity, bool>> propertySelectors, CancellationToken cancellationToken = default)
            => _inner.FindMatchAsync(propertySelectors, cancellationToken);
    }
}
