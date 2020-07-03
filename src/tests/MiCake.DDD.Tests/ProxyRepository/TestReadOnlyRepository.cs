using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Tests.ProxyRepository
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

        public long GetCount()
        => Data.Count;
    }

    public class TestReadOnlyFreeRepository<TEntity, TKey> : IReadOnlyFreeRepository<TEntity, TKey>
          where TEntity : class, IEntity<TKey>
    {
        protected List<TEntity> Data { get; set; }

        public TestReadOnlyFreeRepository()
        {
            Data = new List<TEntity>();
        }

        public TEntity Find(TKey ID)
        => Data.FirstOrDefault(s => s.Id.Equals(ID));

        public Task<TEntity> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var result = Data.FirstOrDefault(s => s.Id.Equals(ID));
            return Task.FromResult(result);
        }

        public long GetCount()
        => Data.Count;

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            long count = Data.Count;
            return Task.FromResult(count);
        }

        public IQueryable<TEntity> FindMatch(Expression<Func<TEntity, object>> propertySelectors)
        => null;

        IQueryable<TEntity> IReadOnlyFreeRepository<TEntity, TKey>.GetAll()
        => Data.AsQueryable();

        Task<IQueryable<TEntity>> IReadOnlyFreeRepository<TEntity, TKey>.GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult(Data.AsQueryable());

        public IQueryable<TEntity> FindMatch(Expression<Func<TEntity, bool>> propertySelectors)
        => Data.AsQueryable().Where(propertySelectors);

        public Task<IQueryable<TEntity>> FindMatchAsync(Expression<Func<TEntity, bool>> propertySelectors, CancellationToken cancellationToken = default)
        {
            var result = Data.AsQueryable().Where(propertySelectors);
            return Task.FromResult(result);
        }
    }
}
