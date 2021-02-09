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
    /// <summary>
    /// A readonly repository for EFCore.
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class EFReadOnlyFreeRepository<TDbContext, TEntity, TKey> :
        EFRepositoryBase<TDbContext, TEntity, TKey>,
        IReadOnlyFreeRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TDbContext : DbContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public EFReadOnlyFreeRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public TEntity Find(TKey ID)
        {
            return DbSet.Find(ID);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TEntity> FindAsync(TKey ID, CancellationToken cancellationToken = default)
        {
            return await DbSet.FindAsync(new object[] { ID }, cancellationToken);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="propertySelectors"></param>
        /// <returns></returns>
        public IQueryable<TEntity> FindMatch(Expression<Func<TEntity, bool>> propertySelectors)
        {
            return DbSet.AsQueryable().Where(propertySelectors);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="propertySelectors"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IQueryable<TEntity>> FindMatchAsync(Expression<Func<TEntity, bool>> propertySelectors, CancellationToken cancellationToken = default)
        {
            var result = DbSet.AsQueryable().Where(propertySelectors);
            return Task.FromResult(result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public IQueryable<TEntity> GetAll()
        {
            return DbSet.AsQueryable();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = DbSet.AsQueryable();
            return Task.FromResult(result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public long GetCount()
        {
            return DbSet.Count();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet.CountAsync(cancellationToken);
        }
    }
}
