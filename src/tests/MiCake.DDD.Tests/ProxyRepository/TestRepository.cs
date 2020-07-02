using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.Tests.ProxyRepository
{
    public class TestRepository<TAggregateRoot, TKey> : TestReadOnlyRepository<TAggregateRoot, TKey>, IRepository<TAggregateRoot, TKey>
           where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        public void Add(TAggregateRoot aggregateRoot)
        => Data.Add(aggregateRoot);

        public TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot, bool autoExecute = true)
        {
            Data.Add(aggregateRoot);
            return aggregateRoot;
        }

        public Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, bool autoExecute = true, CancellationToken cancellationToken = default)
        {
            Data.Add(aggregateRoot);
            return Task.FromResult(aggregateRoot);
        }

        public Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            Data.Add(aggregateRoot);
            return Task.CompletedTask;
        }

        public void Delete(TAggregateRoot aggregateRoot)
        => Data.Remove(aggregateRoot);

        public Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            Data.Remove(aggregateRoot);
            return Task.CompletedTask;
        }

        public void Update(TAggregateRoot aggregateRoot)
        {
            Data.RemoveAll(s => s.Id.Equals(aggregateRoot.Id));
            Data.Add(aggregateRoot);
        }

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            Update(aggregateRoot);
            return Task.CompletedTask;
        }
    }

    public class TestFreeRepository<TEntity, TKey> : TestReadOnlyFreeRepository<TEntity, TKey>, IFreeRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>
    {
        public void Add(TEntity aggregateRoot)
        => Data.Add(aggregateRoot);

        public TEntity AddAndReturn(TEntity entity, bool autoExecute = true)
        {
            Data.Add(entity);
            return entity;
        }

        public Task<TEntity> AddAndReturnAsync(TEntity entity, bool autoExecute = true, CancellationToken cancellationToken = default)
        {
            Data.Add(entity);
            return Task.FromResult(entity);
        }

        public Task AddAsync(TEntity aggregateRoot, CancellationToken cancellationToken = default)
        {
            Data.Add(aggregateRoot);
            return Task.CompletedTask;
        }

        public void Delete(TEntity aggregateRoot)
        => Data.Remove(aggregateRoot);

        public Task DeleteAsync(TEntity aggregateRoot, CancellationToken cancellationToken = default)
        {
            Data.Remove(aggregateRoot);
            return Task.CompletedTask;
        }

        public void Update(TEntity aggregateRoot)
        {
            Data.RemoveAll(s => s.Id.Equals(aggregateRoot.Id));
            Data.Add(aggregateRoot);
        }

        public Task UpdateAsync(TEntity aggregateRoot, CancellationToken cancellationToken = default)
        {
            Update(aggregateRoot);
            return Task.CompletedTask;
        }
    }
}
