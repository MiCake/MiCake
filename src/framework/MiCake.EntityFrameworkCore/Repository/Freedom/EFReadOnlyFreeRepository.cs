using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository.Freedom
{
    public class EFReadOnlyFreeRepository<TDbContext, TEntity, TKey> :
        EFRepositoryBase<TDbContext, TEntity, TKey>,
        IReadOnlyFreeRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TDbContext : DbContext
    {
        public EFReadOnlyFreeRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public TEntity Find(TKey ID)
        {
            return DbSet.Find(ID);
        }

        public async Task<TEntity> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            return await DbSet.FindAsync(new object[] { ID }, cancellationToken);
        }

        public IQueryable<TEntity> FindMatch(Expression<Func<TEntity, bool>> propertySelectors)
        {
            return DbSet.AsQueryable().Where(propertySelectors);
        }

        public Task<IQueryable<TEntity>> FindMatchAsync(Expression<Func<TEntity, bool>> propertySelectors, CancellationToken cancellationToken = default)
        {
            var result = DbSet.AsQueryable().Where(propertySelectors);
            return Task.FromResult(result);
        }

        public IQueryable<TEntity> GetAll()
        {
            return DbSet.AsQueryable();
        }

        public Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = DbSet.AsQueryable();
            return Task.FromResult(result);
        }

        public long GetCount()
        {
            return DbSet.Count();
        }

        public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet.CountAsync(cancellationToken);
        }
    }
}
