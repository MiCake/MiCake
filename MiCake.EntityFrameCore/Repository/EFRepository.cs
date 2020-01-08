using MiCake.DDD.Domain;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    public class EFRepository<TDbContext, TAggregateRoot, TKey> :
        EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey>,
        IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
    {
        public EFRepository(IUnitOfWorkManager uowManager) : base(uowManager)
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

        public Task<TAggregateRoot> AddAndReturnAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
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

        public Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
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
