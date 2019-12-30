using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    public class EFRepository<TDbContext, TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
    {

        public EFRepository()
        {

        }

        public void Add(TAggregateRoot aggregateRoot)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot AddAndReturn(TAggregateRoot aggregateRoot)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot AddAndReturnAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Delete(TAggregateRoot aggregateRoot)
        {
            throw new NotImplementedException();
        }

        public void DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot Find(TKey ID)
        {
            throw new NotImplementedException();
        }

        public Task<TAggregateRoot> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public long GetCount()
        {
            throw new NotImplementedException();
        }

        public void Update(TAggregateRoot aggregateRoot)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
