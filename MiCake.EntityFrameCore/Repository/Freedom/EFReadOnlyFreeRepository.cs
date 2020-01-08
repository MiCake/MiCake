using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository.Freedom
{
    public class EFReadOnlyFreeRepository<TDbContext, TAggregateRoot, TKey> : IFreeRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TDbContext : DbContext
    {
        public void Add(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot AddAndReturn(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot AddAndReturnAsync(TAggregateRoot entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(TAggregateRoot entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Delete(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public void DeleteAsync(TAggregateRoot entity, CancellationToken cancellationToken = default)
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

        public IQueryable<TAggregateRoot> FindMatch(params Expression<Func<TAggregateRoot, object>>[] propertySelectors)
        {
            throw new NotImplementedException();
        }

        public long GetCount()
        {
            throw new NotImplementedException();
        }

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public List<TAggregateRoot> GetList()
        {
            throw new NotImplementedException();
        }

        public Task<List<TAggregateRoot>> GetListAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Update(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(TAggregateRoot entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
