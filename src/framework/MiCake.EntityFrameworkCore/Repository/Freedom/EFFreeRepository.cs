using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository.Freedom
{
    public class EFFreeRepository<TDbContext, TEntity, TKey> :
        EFReadOnlyFreeRepository<TDbContext, TEntity, TKey>,
        IFreeRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TDbContext : DbContext
    {
        public EFFreeRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public void Add(TEntity entity)
        {
            DbSet.Add(entity);
        }

        public TEntity AddAndReturn(TEntity entity, bool autoExecute = false)
        {
            var result = DbSet.Add(entity);

            if (autoExecute)
                DbContext.SaveChanges();

            return result.Entity;
        }

        public async Task<TEntity> AddAndReturnAsync(TEntity entity, bool autoExecute = false, CancellationToken cancellationToken = default)
        {
            var result = await DbSet.AddAsync(entity, cancellationToken);

            if (autoExecute)
                await DbContext.SaveChangesAsync(cancellationToken);

            return result.Entity;
        }

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(entity, cancellationToken);
        }

        public void Delete(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteByIdAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            var item = await DbSet.FindAsync(new object[] { ID }, cancellationToken);
            if (item != null)
                DbSet.Remove(item);
        }

        public void Update(TEntity entity)
        {
            DbSet.Update(entity);
        }

        public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            DbSet.Update(entity);
            return Task.CompletedTask;
        }
    }
}
